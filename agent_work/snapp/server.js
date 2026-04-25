const express = require('express');
const { v4: uuidv4 } = require('uuid');
const path = require('path');
const fs = require('fs');
const { exec } = require('child_process');
const crypto = require('crypto');

const app = express();
const PORT = 3082;

const GENERATED_DIR = path.join(__dirname, 'generated');
const CACHE_FILE = path.join(__dirname, 'cache.json');
const MMX_CACHE_FILE = path.join(__dirname, 'mmx_cache.json');

// Ensure directories exist
fs.mkdirSync(GENERATED_DIR, { recursive: true });

// Load caches
function loadCache(file) {
  try { return JSON.parse(fs.readFileSync(file, 'utf8')); }
  catch { return {}; }
}
function saveCache(file, data) {
  fs.writeFileSync(file, JSON.stringify(data, null, 2));
}

const imageCache = loadCache(CACHE_FILE);
const mmxCache = loadCache(MMX_CACHE_FILE);

// Middleware
app.use(express.json());
app.use(express.static(path.join(__dirname, 'public')));
app.use('/generated', express.static(GENERATED_DIR));

// --- Widget Style Prompt ---
const WIDGET_STYLE = 'flat vector illustration on white background, simple shapes, minimal detail, clear black outlines, solid colors, children book illustration style, no shading, no gradient, clean edges';

function buildPrompt(concept, context) {
  const full = context ? `${concept}, ${context}` : concept;
  return `${full}, ${WIDGET_STYLE}`;
}

// --- MiniMax Image Generation via mmx CLI ---
function generateWithMMX(prompt) {
  return new Promise((resolve, reject) => {
    const cacheKey = crypto.createHash('md5').update(prompt).digest('hex');

    // Check prompt-level cache
    if (mmxCache[cacheKey]) {
      const cachedPath = path.join(GENERATED_DIR, mmxCache[cacheKey]);
      if (fs.existsSync(cachedPath)) {
        return resolve({ filename: mmxCache[cacheKey], cached: true });
      }
    }

    const jobId = uuidv4();
    const prefix = `tmp_${jobId.slice(0, 8)}`;
    const outDir = GENERATED_DIR;

    // Escape prompt for shell — wrap in double quotes, escape inner double quotes
    const escapedPrompt = prompt
      .replace(/\\/g, '\\\\')
      .replace(/"/g, '\\"');

    const cmd = [
      'mmx', 'image', 'generate',
      '--prompt', `"${escapedPrompt}"`,
      '--aspect-ratio', '1:1',
      '--out-dir', `"${outDir}"`,
      '--out-prefix', prefix
    ].join(' ');

    exec(cmd, { timeout: 0 /* no timeout - MMX can be slow */ }, (err, stdout, stderr) => {
      // Find the generated file
      const tmpFiles = fs.readdirSync(GENERATED_DIR)
        .filter(f => f.startsWith(prefix) && f.endsWith('.jpg'))
        .map(f => ({
          name: f,
          mtime: fs.statSync(path.join(GENERATED_DIR, f)).mtime
        }))
        .sort((a, b) => b.mtime - a.mtime);

      if (tmpFiles.length === 0) {
        const errMsg = stderr || (err ? err.message : 'Ingen bildfil hittades');
        return reject(new Error('MMX error: ' + errMsg));
      }

      const tmpFile = tmpFiles[0].name;
      const finalFilename = `${uuidv4()}.jpg`;
      const tmpPath = path.join(GENERATED_DIR, tmpFile);
      const finalPath = path.join(GENERATED_DIR, finalFilename);

      fs.renameSync(tmpPath, finalPath);

      mmxCache[cacheKey] = finalFilename;
      saveCache(MMX_CACHE_FILE, mmxCache);

      resolve({ filename: finalFilename, cached: false });
    });
  });
}

// --- API Routes ---
app.post('/api/generate', async (req, res) => {
  const { concept, context } = req.body;
  if (!concept) return res.status(400).json({ error: 'Koncept krävs' });

  const cacheKey = concept.toLowerCase().trim();

  // Check local cache first
  if (imageCache[cacheKey]) {
    const entry = imageCache[cacheKey];
    if (fs.existsSync(path.join(GENERATED_DIR, entry.filename))) {
      return res.json({ filename: entry.filename, cached: true });
    }
  }

  const prompt = buildPrompt(concept, context);
  let result;
  try {
    result = await generateWithMMX(prompt);
  } catch (err) {
    return res.status(500).json({ error: 'Genereringsfel: ' + err.message });
  }

  // Update local cache
  imageCache[cacheKey] = { filename: result.filename, generated: new Date().toISOString() };
  saveCache(CACHE_FILE, imageCache);

  res.json({ filename: result.filename, cached: result.cached });
});

app.get('/api/list', (req, res) => {
  res.json(imageCache);
});

app.listen(PORT, () => {
  console.log(`Snäpp running at http://localhost:${PORT}`);
});
