Image assets folder

This folder holds generated images used by the project. Do NOT add binary images here unless they are final assets.

Where to place generated images
- Save final PNGs into: assets\images\
- Use the filenames suggested in IMAGE_PROMPTS.txt (or similar descriptive names).

Example CLI invocations
1) Stable Diffusion (CompVis-style script)
python scripts\txt2img.py --prompt "<your prompt>" --outdir "assets/images" --H 768 --W 1280 --n_samples 1 --n_iter 1 --scale 7.5 --ddim_steps 28 --seed 123456

2) AUTOMATIC1111 WebUI (script usage)
# From webui root, run: python launch.py
# Use the txt2img web UI or run the script interface (if installed):
python scripts\txt2img.py --prompt "<your prompt>" --outdir "outputs/txt2img-images" --skip_grid --n_iter 1 --n_samples 1 --ddim_steps 28 --scale 7.5 --seed 123456
# Move results to repo: mv outputs\txt2img-images\*.png assets\images\

Tips
- Start with 512-768px short edge for thumbnails; 1280px+ for banners/hero images.
- Match aspect ratio in prompts (1:1, 3:2, 16:9, 21:9).
- Keep a copy of seed and generation parameters in IMAGE_PROMPTS.txt for reproducibility.

Committing final assets
- Final approved images may be added and committed to the repository under assets\images\.
- If files are large, consider using Git LFS per project policy.
