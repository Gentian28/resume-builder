const fs = require('node:fs');
const path = require('node:path');

const owner = 'microsoft';
const repo = 'winget-pkgs';
const packageId = 'Gentian28.ResumeBuilder';

// Retry safely after a maintainer submits manually or a previous run loses its response.
module.exports = async function submissionStatus({ github, version, manifestRoot }) {
  if (!/^\d+\.\d+\.\d+$/.test(version)) throw new Error('Version must be x.y.z.');
  const prefix = `manifests/g/Gentian28/ResumeBuilder/${version}/`;
  const files = fs.readdirSync(manifestRoot).filter(name => name.endsWith('.yaml')).sort();
  if (files.length !== 3) throw new Error('Expected three local manifest files.');

  const content = async (ref, name, repository = { owner, repo }) => {
    const result = await github.rest.repos.getContent({ ...repository, path: prefix + name, ref });
    if (result.data.type !== 'file' || result.data.encoding !== 'base64') {
      throw new Error('Expected a manifest file from GitHub.');
    }
    return Buffer.from(result.data.content, 'base64').toString('utf8');
  };
  const normalize = text => text.replace(/\r\n/g, '\n').trimEnd();
  const verify = async (ref, repository) => {
    for (const name of files) {
      if (normalize(await content(ref, name, repository)) !== normalize(fs.readFileSync(path.join(manifestRoot, name), 'utf8'))) {
        throw new Error(`Existing upstream submission differs: ${name}. Review it before retrying.`);
      }
    }
  };

  try {
    await github.rest.repos.getContent({ owner, repo, path: prefix.slice(0, -1) });
  } catch (error) {
    if (error.status !== 404) throw error;
    // Search open submissions only after confirming this version is not indexed.
    const results = await github.paginate(github.rest.search.issuesAndPullRequests, {
      q: `repo:${owner}/${repo} is:pr is:open "${packageId}" "${version}" in:title`,
      per_page: 100,
    });
    const exactVersion = new RegExp(`(^|[^0-9.])${version.replaceAll('.', '\\.')}([^0-9.]|$)`);
    for (const candidate of results.filter(item => item.title.includes(packageId) && exactVersion.test(item.title))) {
      const changed = await github.paginate(github.rest.pulls.listFiles, { owner, repo, pull_number: candidate.number, per_page: 100 });
      if (!changed.some(file => file.filename.startsWith(prefix))) continue;
      if (changed.length !== files.length || changed.some(file => !files.some(name => file.filename === prefix + name))) {
        throw new Error(`Open PR ${candidate.html_url} changes additional files. Review it before retrying.`);
      }
      const pull = await github.rest.pulls.get({ owner, repo, pull_number: candidate.number });
      const head = pull.data.head;
      if (!head.repo) throw new Error('Existing submission fork is unavailable. Review it before retrying.');
      await verify(head.sha, { owner: head.repo.owner.login, repo: head.repo.name });
      return { submit: false, reason: `Already submitted: ${candidate.html_url}` };
    }
    return { submit: true, reason: 'No indexed version or existing matching submission.' };
  }
  await verify(undefined);
  return { submit: false, reason: `Already indexed: https://github.com/${owner}/${repo}/tree/master/${prefix}` };
};
