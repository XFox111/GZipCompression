# GZipCompression
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
![GitHub repo size](https://img.shields.io/github/repo-size/xfox111/GZipCompression?label=Repository%20size)

![Twitter Follow](https://img.shields.io/twitter/follow/xfox111?style=social)
![GitHub followers](https://img.shields.io/github/followers/xfox111?label=Follow%20@xfox111&style=social)

Multi-thread console program which is used to compress/decompress files using block-by-block compression. I've done this project as an entrance test for Software developer position in Veeam Software
## Features:
- Compression by separate 1MB blocks
- Decompression
- Can decompress only those archives which have been compressed by the archiver
- Archives cannot be unpacked with other archivers
- Very fast (see "Test results" below)
## Usage
1. Unzip package
2. Open Command prompt (or PowerShell)
3. Navigate to program's folder (`cd "{ABSOLUTE_FOLDER_PATH}"`)
4. Type `GZipTest.exe [compress | decompress] {ABSOLUTE_SOURCE_FILE_PATH} {ABSOLUTE_DESTINATION_FILE_PATH}`
5. Press Enter
## Test results
### Test machine specs
- Intel Core i5-2300 2,8 GHz 4 threads
- 10 ГБ ОЗУ DDR3 1333 MHz
- WDC WDS240G2G0A-00JH30 – SSD
- Radeon RX 580
#### WinRAR archive (500MB)
- Compressed in: 5 seconds
- Decompressed in 1 seconds
#### WinRAR archive (42GB)
- Compressed in: 10 minutes
- Decompressed in 6 minutes
## Copyrights
> ©2019 Michael "XFox" Gordeev
