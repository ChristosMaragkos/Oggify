# Oggify
Oggify is a lightweight command-line tool for batch converting `.wav` or `.mp3` files to `.ogg` using `FFmpeg`.

## Features
- Batch convert WAV or MP3 to OGG
- Recursive subdirectory support
- Optionally delete original files after conversion
- Saves FFmpeg path on first launch

## Usage
```sh
Oggify.exe <input_directory> [options]
```

### Options:
- `--overwrite`        Overwrite existing .ogg files
- `--skip-existing`    Skip conversion if .ogg exists
- `--recursive`        Search subdirectories recursively
- `--replace`          Delete original audio files after conversion
- `--from-mp3`         Convert `.mp3` instead of `.wav`
- `ffmpeg <path>`      Set path to `ffmpeg.exe`

### Example:
```sh
Oggify.exe "C:\MySamples" --recursive --overwrite --replace
```
Running this results in:
- All `.wav` files in `C:\MySamples` and its subdirectories to be converted to `.ogg`
- All of the already existing `.ogg` files with a `.wav` file of the same name to be overwritten
- All of the original `.wav` files to be deleted upon conversion to save disk space

## First-time Setup
On first run, the app will ask for your `FFmpeg` executable path. This is saved locally to `ffmpeg_path.txt`.

<sup><sub>If you want to change this later, you can do it via the ffmpeg (path) command!</sub></sup>

## Build
No need to build Oggify from the source code! Just go to [Releases](https://github.com/WhiteTowerGames/Oggify/releases) and download the latest version from there!

## Other
Pull requests **highly welcome**! I created Oggify to make it part of my gamedev workflow, as I found myself constantly opening up Audacity for a single export at a time.
So if there is a feature you'd like Oggify to have do not hesitate to open a PR so we can take a look together!

### Credits
White Tower Games / Christos Maragkos - Programming, Testing
