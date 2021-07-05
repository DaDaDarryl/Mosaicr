# Mosaicr
A command line app to create a mosaic of an input image generated from a folder of images.

## Description
Written in .Net Core 3.51 on MacOS Big Sur 11.4.

## Getting Started

### Dependencies
* Microsoft .Net Core 3.51
* MacOS requires [libgdiplus packaging for macOS](https://github.com/CoreCompat/libgdiplus-packaging)

### Running the Program
* From a command promp/terminal: dotnet Mosaicr.dll [options]
* Example usage
   * Display help: dotnet Mosaicr.dll -h
   * Default options: dotnet Mosaicr.dll -i sunset.jpg -o "sunset mosaic.jpg" -s path/to/folder/of/jpegs
   * Specifying mosaic size of 40x60 blocks: dotnet Mosaicr.dll -i sunset.jpg -o "sunset mosaic.jpg" -s path/to/folder/of/jpegs -bh 40 -bv 60
   * Specifying output jpeg quality of 80%: dotnet Mosaicr.dll -i sunset.jpg -o "sunset mosaic.jpg" -s path/to/folder/of/jpegs -q 80

## Command Line Options
### Required
* input: Path including filename of the image to mosaic
* output: Path including filename of the generated mosaic JPEG image
* sourceFolder: Path to a folder containing images to generate the mosaic. Can include subfolders

### Optional
* blocksHorizontal: Number of mosaic blocks horizontally. Default=20
* blocksVertical: Number of mosaic blocks vertically. Default=20
* quality: Quality of the output image: 0-100. Default=70 (0=lowest quality, smaller file size, 100=highest quality, larger file size)

## Author
Darryl de Wet  
[@DaDaDarryl](https://twitter.com/dadadarryl)

## Version History
* 0.1
    * Initial Release
