"""Exercise real release packages on disposable GitHub-hosted native runners only.

The previous app creates its own database. Synthetic rows are inserted into that
schema, then the actual Velopack updater applies the new package. This deliberately
does not use the current EF model to manufacture an old database.
"""

import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import shutil
import sqlite3
import subprocess
import sys
import time
import uuid
import xml.etree.ElementTree as ET


def run(*args, **kwargs):
    print("+", *map(str, args), flush=True)
    return subprocess.run(list(map(str, args)), check=True, timeout=240, **kwargs)


def verify_checksums(directory, rid):
    for line in (directory / f"SHA256SUMS-{rid}.txt").read_text().splitlines():
        digest, name = line.split(maxsplit=1)
        asset = directory / name.lstrip("*")
        assert asset.parent == directory and asset.is_file(), name
        assert hashlib.file_digest(asset.open("rb"), "sha256").hexdigest() == digest, name


def app_bundle():
    matches = [p for base in (Path("/Applications"), Path.home() / "Applications")
               for p in base.glob("*.app")
               if (p / "Contents/MacOS/ResumeBuilder.App").is_file()]
    assert len(matches) == 1, matches
    return matches[0]


def stop_installer_launch(executable):
    # The macOS pkg's postinstall opens the app. Stop that exact executable
    # on this disposable runner before beginning the controlled launch checks.
    time.sleep(3)
    result = subprocess.run(["pkill", "-f", f"^{re.escape(str(executable))}($| )"], check=False)
    assert result.returncode in (0, 1)
    time.sleep(1)


def manifest_version(directory):
    manifests = list(directory.rglob("sq.version"))
    assert len(manifests) == 1, manifests
    root = ET.parse(manifests[0]).getroot()
    return next(e.text for e in root.iter() if e.tag.split("}")[-1] == "version")


def start_and_stop(executable, log, env, database=None):
    with log.open("wb") as output:
        proc = subprocess.Popen([str(executable)], env=env, stdout=output, stderr=output)
        try:
            # Wait for schema creation as well as process survival. A recovery dialog
            # staying alive is not proof that the database initialized successfully.
            for _ in range(30):
                time.sleep(1)
                assert proc.poll() is None, f"App exited early: {proc.returncode}; see {log}"
                if database and database.is_file():
                    with sqlite3.connect(database) as db:
                        if db.execute("SELECT count(*) FROM sqlite_master WHERE name='Resumes'").fetchone()[0]:
                            break
            assert proc.poll() is None
            time.sleep(5)
            assert proc.poll() is None, f"App crashed; see {log}"
        finally:
            if proc.poll() is None:
                proc.terminate()
                try:
                    proc.wait(timeout=15)
                except subprocess.TimeoutExpired:
                    proc.kill()
                    proc.wait(timeout=10)


def seed_old_database(path):
    arrays = {"Experiences", "EducationList", "Skills", "Languages", "Certifications", "Projects", "CustomSections"}
    snapshot = {}
    with sqlite3.connect(path) as db:
        assert db.execute("PRAGMA integrity_check").fetchone()[0] == "ok"
        for table in ("Resumes", "CoverLetters", "JobApplications"):
            columns = db.execute(f'PRAGMA table_info("{table}")').fetchall()
            assert columns, f"Previous app did not initialize {table}"
            row = {}
            for _, name, kind, required, default, primary in columns:
                if primary:
                    value = 901
                elif name in ("RowVersion", "SyncId"):
                    value = str(uuid.uuid4())
                elif name in ("CreatedAt", "UpdatedAt", "AppliedOn", "LetterDate"):
                    value = "2026-10-15 10:00:00"
                elif name in arrays:
                    value = "[]"
                elif name == "Paragraphs":
                    value = '["Synthetic upgrade acceptance letter."]'
                elif name in ("TemplateSettings", "SectionOrder"):
                    value = "{}"
                elif name == "SelectedTemplateId":
                    value = "modern" if table == "Resumes" else "letter-modern"
                elif name == "Status":
                    value = "Applied"
                elif name == "ResumeId":
                    value = 901
                elif name == "Name":
                    value = f"Synthetic upgrade acceptance {table}"
                elif name == "Company":
                    value = "Example Company"
                elif name == "PersonalInfo_FirstName":
                    value = "Jane"
                elif name == "PersonalInfo_LastName":
                    value = "Doe"
                elif name == "PersonalInfo_Email":
                    value = "jane@example.com"
                elif name == "AccentColor":
                    value = "#4F46E5"
                elif name == "FontFamily":
                    value = "Inter"
                elif not required:
                    value = None
                else:
                    value = 0 if "INT" in kind.upper() else ""
                row[name] = value
            names = ",".join(f'"{c}"' for c in row)
            db.execute(f'INSERT INTO "{table}" ({names}) VALUES ({",".join("?" for _ in row)})', list(row.values()))
            snapshot[table] = row
    return snapshot


def verify_data(path, snapshot, upgraded):
    with sqlite3.connect(path) as db:
        db.row_factory = sqlite3.Row
        assert db.execute("PRAGMA integrity_check").fetchone()[0] == "ok"
        for table, before in snapshot.items():
            after = dict(db.execute(f'SELECT * FROM "{table}" WHERE Id=901').fetchone())
            assert all(after[k] == v for k, v in before.items()), f"{table} data changed"
        if upgraded:
            resume_columns = {r[1] for r in db.execute('PRAGMA table_info("Resumes")')}
            app_columns = {r[1] for r in db.execute('PRAGMA table_info("JobApplications")')}
            assert "BaseResumeSyncId" in resume_columns
            assert {"SentResumePdf", "SentCoverLetterPdf", "NextActionOn", "Contact"} <= app_columns
            # Do not fabricate a historical sent snapshot for an old application.
            historical = db.execute('SELECT SentResumeJson, SentResumePdf FROM JobApplications WHERE Id=901').fetchone()
            assert tuple(historical) == (None, None)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--rid", required=True)
    parser.add_argument("--version", required=True)
    parser.add_argument("--previous", default="1.2.3")
    parser.add_argument("--artifacts", type=Path, required=True)
    args = parser.parse_args()
    assert os.environ.get("GITHUB_ACTIONS") == "true", "Only run on disposable GitHub-hosted runners"
    assert os.environ.get("RUNNER_ENVIRONMENT") == "github-hosted", "Never use a persistent self-hosted runner"
    expected_arch = "arm64" if args.rid.endswith("arm64") else "x86_64"
    actual_arch = platform.machine().lower().replace("amd64", "x86_64").replace("aarch64", "arm64")
    assert actual_arch == expected_arch, (actual_arch, args.rid)
    artifacts = args.artifacts.resolve()
    verify_checksums(artifacts, args.rid)
    root = Path(os.environ["RUNNER_TEMP"]) / f"resume-acceptance-{uuid.uuid4().hex}"
    root.mkdir()
    evidence = artifacts.parent / "acceptance-evidence"
    evidence.mkdir(exist_ok=True)
    old = root / "previous"
    old.mkdir()
    suffix = {"win-x64": "win-Setup.exe", "linux-x64": ".AppImage", "osx-arm64": "osx-Setup.pkg", "osx-x64": "osx-x64-Setup.pkg"}[args.rid]
    name = f"ResumeBuilder{suffix}" if suffix.startswith(".") else f"ResumeBuilder-{suffix}"
    run("gh", "release", "download", f"v{args.previous}", "--repo", "Gentian28/resume-builder", "--pattern", name,
        "--pattern", f"SHA256SUMS-{args.rid}.txt", "--dir", old)
    # Verify the downloaded installer against the previous release's checksums too.
    previous_lines = (old / f"SHA256SUMS-{args.rid}.txt").read_text().splitlines()
    expected = next(line.split()[0] for line in previous_lines if line.split()[-1].lstrip("*") == name)
    assert hashlib.file_digest((old / name).open("rb"), "sha256").hexdigest() == expected
    env = os.environ.copy()
    env.pop("RESUMEBUILDER_DATA_DIRECTORY", None)
    if sys.platform == "win32":
        database = Path(os.environ["LOCALAPPDATA"]) / "ResumeBuilder/resumes.db"
    elif sys.platform == "darwin":
        database = Path.home() / "Library/Application Support/ResumeBuilder/resumes.db"
    else:
        database = Path(env.get("XDG_DATA_HOME", str(Path.home() / ".local/share"))) / "ResumeBuilder/resumes.db"
    assert not database.exists(), f"Acceptance profile was not empty: {database}"
    if sys.platform == "win32":
        install = root / "installed"
        run(old / name, "--silent", "--installto", install, "--log", evidence / "install.log")
        executable = install / "current/ResumeBuilder.App.exe"
        updater = install / "Update.exe"
    elif sys.platform == "darwin":
        run("sudo", "installer", "-pkg", old / name, "-target", "/")
        install = app_bundle()
        executable = install / "Contents/MacOS/ResumeBuilder.App"
        updater = install / "Contents/MacOS/UpdateMac"
        stop_installer_launch(executable)
    else:
        install = root / "ResumeBuilder.AppImage"
        shutil.copy2(old / name, install)
        install.chmod(0o755)
        run(install, "--appimage-extract", cwd=root, stdout=subprocess.DEVNULL)
        bundle = root / "squashfs-root"
        executable = bundle / "usr/bin/ResumeBuilder.App"
        updater = bundle / "usr/bin/UpdateNix"
        env["APPIMAGE"] = str(install)
    start_and_stop(executable, evidence / "previous-startup.log", env, database)
    assert database.exists(), f"Previous app did not create {database}"
    snapshot = seed_old_database(database)
    verify_data(database, snapshot, False)
    packages = list(artifacts.glob("*-full.nupkg"))
    assert len(packages) == 1, packages
    # For /Applications installations the updater needs administrative access.
    # CI supplies it explicitly; this does not test a human Gatekeeper/admin prompt.
    prefix = ["sudo"] if sys.platform == "darwin" else []
    run(*prefix, updater, "--rootDir", install, "--verbose", "--log", evidence / "update.log",
        "apply", "--norestart", "--package", packages[0])
    if sys.platform.startswith("linux"):
        # Re-extract the updated image into a different directory. Never launch
        # stale files from the previous extraction after the AppImage is replaced.
        current = root / "updated"
        current.mkdir()
        run(install, "--appimage-extract", cwd=current, stdout=subprocess.DEVNULL)
        bundle = current / "squashfs-root"
        executable = bundle / "usr/bin/ResumeBuilder.App"
        version = manifest_version(bundle)
    else:
        version = manifest_version(install / "current" if sys.platform == "win32" else install)
    assert version == args.version, (version, args.version)
    start_and_stop(executable, evidence / "updated-startup.log", env, database)
    verify_data(database, snapshot, True)
    backups = list(database.parent.glob("resumes.backup-*.db"))
    assert backups, "Upgrade did not create a safety database backup"
    verify_data(max(backups, key=lambda p: p.stat().st_mtime), snapshot, False)
    # Also rehearse restart and schema idempotence after the first upgraded launch.
    start_and_stop(executable, evidence / "restart.log", env, database)
    verify_data(database, snapshot, True)
    # Exercise the new installer too, separately from the update package.
    if sys.platform == "win32":
        fresh = root / "fresh-install"
        run(artifacts / name, "--silent", "--installto", fresh, "--log", evidence / "fresh-install.log")
        fresh_executable = fresh / "current/ResumeBuilder.App.exe"
        assert manifest_version(fresh / "current") == args.version
    elif sys.platform == "darwin":
        run("sudo", "installer", "-pkg", artifacts / name, "-target", "/")
        fresh_executable = app_bundle() / "Contents/MacOS/ResumeBuilder.App"
        stop_installer_launch(fresh_executable)
        assert manifest_version(app_bundle()) == args.version
    else:
        fresh = root / "fresh-install"
        fresh.mkdir()
        fresh_image = fresh / name
        shutil.copy2(artifacts / name, fresh_image)
        fresh_image.chmod(0o755)
        run(fresh_image, "--appimage-extract", cwd=fresh, stdout=subprocess.DEVNULL)
        fresh_executable = fresh / "squashfs-root/usr/bin/ResumeBuilder.App"
        env["APPIMAGE"] = str(fresh_image)
        assert manifest_version(fresh / "squashfs-root") == args.version
    fresh_profile = root / "fresh-profile"
    env["RESUMEBUILDER_DATA_DIRECTORY"] = str(fresh_profile)
    start_and_stop(fresh_executable, evidence / "fresh-startup.log", env, fresh_profile / "resumes.db")
    with sqlite3.connect(fresh_profile / "resumes.db") as db:
        assert db.execute("PRAGMA integrity_check").fetchone()[0] == "ok"
        assert db.execute('SELECT count(*) FROM Resumes').fetchone()[0] == 0
    result = {"rid": args.rid, "native_architecture": actual_arch, "previous_version": args.previous,
              "installed_version": version, "old_schema_created_by_previous_app": True,
              "preserved_tables": list(snapshot), "safety_backup_verified": True,
              "native_startup_and_restart": True, "new_installer_startup": True, "unsigned": True,
              "scope": "Installer/AppImage and real updater; not Gatekeeper, SmartScreen or physical DPI acceptance"}
    (evidence / "result.json").write_text(json.dumps(result, indent=2) + "\n")
    print(json.dumps(result, indent=2), flush=True)


if __name__ == "__main__":
    main()
