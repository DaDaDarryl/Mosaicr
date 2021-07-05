# Mosaicr
Create a mosaic of an input image generated from a folder of images.

## Description
Written in .Net Core 3.51 on MacOS Big Sur 11.4.

## Getting Started

### Dependencies
* Microsoft .Net Core 3.51

### Running the Program
* dotnet Mosaicr.dll [options]
* Example usage
** Default options: dotnet Mosaicr.dll -i sunset.jpg -o "sunset mosaic.jpg" -s path/to/folder/of/jpegs
** Specifying mosaic size of 40x60 blocks: dotnet Mosaicr.dll -i sunset.jpg -o "sunset mosaic.jpg" -s path/to/folder/of/jpegs -bh 40 -bv 60
** Specifying output jpeg quality of 80%: dotnet Mosaicr.dll -i sunset.jpg -o "sunset mosaic.jpg" -s path/to/folder/of/jpegs -q 80

## Help
Options:
Required:
-i
--input			        Path including filename of the image to mosaic
-o
--output		        Path including filename of the generated mosaic JPEG image
-s
--sourceFolder		  Path to a folder containing images to generate the mosaic. Can include subfolders

Optional:
-bh
--blocksHorizontal	Number of mosaic blocks horizontally. Default=20
-bv
--blocksVertical	  Number of mosaic blocks vertically. Default=20
-q
--quality		        Quality of the output image: 0-100. Default=70 (0=lowest quality, smaller file size, 100=highest quality, larger file size)

## Authors
Darryl de Wet  
ex. [@DaDaDarryl](https://twitter.com/dadadarryl)

## Version History
* 0.1
    * Initial Release

## License
This project is licensed under the GNU License - see the LICENSE.md file for details
