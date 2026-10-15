const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const check = require('./winget-submission-status.cjs');

function fixture(t, options = {}) {
  const manifestRoot = fs.mkdtempSync(path.join(os.tmpdir(), 'winget-status-'));
  t.after(() => fs.rmSync(manifestRoot, { recursive: true, force: true }));
  const files = ['Gentian28.ResumeBuilder.yaml', 'Gentian28.ResumeBuilder.installer.yaml', 'Gentian28.ResumeBuilder.locale.en-US.yaml'];
  files.forEach(name => fs.writeFileSync(path.join(manifestRoot, name), `PackageVersion: 1.3.0\r\n# ${name}\r\n`));
  const prefix = 'manifests/g/Gentian28/ResumeBuilder/1.3.0/';
  const requests = [];
  const getContent = async args => {
    requests.push(args);
    if (args.path === prefix.slice(0, -1)) {
      if (options.indexError) throw Object.assign(new Error('GitHub error'), { status: options.indexError });
      if (!options.indexed) throw Object.assign(new Error('Not found'), { status: 404 });
      return { data: [] };
    }
    const name = path.posix.basename(args.path);
    const text = options.different ? 'different release' : fs.readFileSync(path.join(manifestRoot, name), 'utf8').replaceAll('\r\n', '\n');
    return { data: { type: 'file', encoding: 'base64', content: Buffer.from(text).toString('base64') } };
  };
  const search = () => {};
  const listFiles = () => {};
  const github = {
    rest: { repos: { getContent }, search: { issuesAndPullRequests: search }, pulls: {
      listFiles, get: async () => ({ data: { head: { sha: 'exact-commit', repo: { owner: { login: 'maintainer' }, name: 'winget-pkgs' } } } }),
    } },
    paginate: async method => method === search ? (options.candidates ?? []) :
      [...files.map(name => ({ filename: prefix + name })), ...(options.additional ? [{ filename: 'unrelated.yaml' }] : [])],
  };
  return { github, version: '1.3.0', manifestRoot, requests };
}
const candidate = { title: 'New version: Gentian28.ResumeBuilder version 1.3.0', number: 1, html_url: 'https://github.com/microsoft/winget-pkgs/pull/1' };

test('an indexed identical version is skipped despite newline differences', async t => {
  const input = fixture(t, { indexed: true });
  assert.equal((await check(input)).submit, false);
});
test('a matching open PR is skipped using its exact fork commit', async t => {
  const input = fixture(t, { candidates: [candidate] });
  assert.equal((await check(input)).submit, false);
  assert.ok(input.requests.slice(1).every(request => request.owner === 'maintainer' && request.ref === 'exact-commit'));
});
test('an absent version is submitted and another version is ignored', async t => {
  const input = fixture(t, { candidates: [{ ...candidate, title: candidate.title.replace('1.3.0', '1.3.0.1') }] });
  assert.equal((await check(input)).submit, true);
});
test('a changed open submission blocks a duplicate', async t => {
  await assert.rejects(check(fixture(t, { candidates: [candidate], different: true })), /differs/);
});
test('a PR containing unrelated manifests blocks a duplicate', async t => {
  await assert.rejects(check(fixture(t, { candidates: [candidate], additional: true })), /additional files/);
});
test('an indexed different version blocks a new submission', async t => {
  await assert.rejects(check(fixture(t, { indexed: true, different: true })), /differs/);
});
test('GitHub auth and network failures never mean absent', async t => {
  for (const indexError of [401, 403, 500]) {
    await assert.rejects(check(fixture(t, { indexError })), /GitHub error/);
  }
});
test('version input cannot escape the manifest directory', async t => {
  await assert.rejects(check({ ...fixture(t), version: '../1.3.0' }), /x.y.z/);
});
