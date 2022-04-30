# REFIS

**Resilient File Storage**

REFIS Encodes any given file in a way so it can still be recovered
even when the underlying file system structure is completely destroyed.
This works for as long as the file itself has not been overwritten,
but it can be heavily fragmented and will still recover.

## CAUTION

REFIS is no replacement for a backup.
REFIS will only recover files if all headers are still intact.

## Usage

This is a command line utility with a help.
Run it with `/?` or no arguments at all to get a quick help.

## Operation modes

This chapter explains the various modes of operation

### Encode `/E`

Stores a file in REFIS format.

Command: `/E [/Y] <infile> <outfile>`

Arguments:

- `/Y`: Overwrite `outfile` if it exists
- `infile`: The file you want to protect
- `outfile`: Where to save the encoded format

### Decode `/D`

Converts a REFIS file back into a regular file.

Command: `/D [/Y] <infile> [outfile]`

Arguments:

- `/Y`: Overwrite `outfile` if it exists
- `infile`: REFIS encoded file
- `outfile`: Decoded file

The file name will be recovered from the REFIS header if `outfile` is not specified.
This will save the restored file in the current working directory.
`outfile` may also be a valid directory to still use the name from the header,
but store the file in the given directory instead of the working directory.

### Info `/I`

Reads a REFIS file and displays information about the information contained within.

Command: `/I <infile>`

Arguments:

- `infile`: A REFIS file

The output will contain the file id, real file name, and file size.
This command expects the file to be the result of /E mode.
To get information about REFIS chunks in a disk image, use /S and /L mode.

### Scan `/S`

Scans a file for valid REFIS chunks.
Writes metadata and other information to an index file.
The index file can then be used in recovery mode to restore files.

Command: `/S [/Y] <infile> <indexfile>`

Arguments:

- `/Y`: Overwrite `indexfile` if it exists
- `infile`: A file with REFIS chunks, possibly a disk image
- `indexfile`: File to store the header index

Note: This only works properly if the source file is a "raw" disk image.
For this command to find headers they must be aligned to a 512 byte boundary or a multiple of it.

### List `/L`

Shows all files contained in an index.
The output contains id, name, size, and recoverability

Command: `/L <indexfile>`

Arguments:

- `indexfile`: Header index file created with /S

### Restore `/R`

Restores a file from an index

Command: `/R [/Y] <infile> <indexfile> <id> [outfile]`

Arguments:

- `/Y`: Overwrite `outfile` if it exists
- `infile`: File with REFIS chunks (same "infile" as in /S command)
- `indexfile`: Header index file
- `id`: Id of the file to restore (Mode /L will display them)
- `outfile`: Filename of recovered file

The bahavior for a missing outfile argument is explained in the Decode chapter above.

## REFIS File Format

REFIS files are a concatenation of headers.
Each header is exactly 512 bytes long.
REFIS files have two headers, a master header at the start of the file,
and a shorter slave header every 512 bytes.

A typical REFIS file therefore looks like this: `MASTER SLAVE SLAVE SLAVE ...`

**All numerical values are stored in big endian mode (network byte order)**

### Master Header

The master header format is as follows:

| Value   | Offset | Size | Description                              |
|---------|--------|------|------------------------------------------|
| "REFIS" |      0 |    5 | The literal ASCII string "REFIS"         |
| Type    |      5 |    1 | Header type.                             |
| Id      |      6 |   16 | Random but identical in master and slave |
| Create  |     22 |    8 | Creation time of the original file       |
| Change  |     30 |    8 | Last write time of the original file     |
| Size    |     38 |    8 | File size                                |
| Name    |     46 | ?    | File name (no path info)                 |
| Null    | ?      |    1 | Null terminator                          |
| Padding | ?      | ?    | Bytes to fill up to 512 bytes            |

The total size of this header is always 512 bytes.

The master header never contains file data.

#### Type

This is `0` to indicate a master header and `1` to indicate a slave header.
Future versions of headers may be using other ids.

#### Id

Although officially random, the size has been specifically chosen to fit a UUID.
This provides adequate collision resistance and duplicate detection.
The value should be different in every REFIS file
but should be identical across all headers within the same file.

#### Create and Change

The value is stored as the `DateTime.Ticks` property of the UTC timestamp.

#### Size

This is the actual file size of the source file.
This is important because REFIS files are always a multiple of 512 bytes in length.
Any extra data that may be at the end due to the 512 byte alignment
and the actual size of the file is undefined and should not be evaluated.

#### Name

The name of the source file as UTF-8 encoded string.
It must not contain nullbytes.

The file name length is only limited by the available space in the header.
Currently this is `512-5-1-16-8-8-8-1=465` bytes.
If more fields are added in the future, this value will be reduced accordingly.

The name should not contain path information, and decoders must discard it.

#### Null

A single nullbyte to terminate the "Name" field.

#### Padding

Padding to fill up the header to 512 bytes.
This may not exist if the "Name" is so long that the header is already 512 bytes.

Padding bytes must be nullbytes.

### Slave Header

The slave header format is as follows:

| Value   | Offset | Size | Description                              |
|---------|--------|------|------------------------------------------|
| "REFIS" |      0 |    5 | The literal ASCII string "REFIS"         |
| Type    |      5 |    1 | Header type.                             |
| Id      |      6 |   16 | Random but identical in master and slave |
| Index   |     22 |    8 | Header index                             |
| Data    |     30 |  482 | File data                                |

#### REFIS, Type, Id

*See "Master Header" for explanation*

#### Index

This is the header index.
It starts at 1 and increments for every slave header written.

This index is used to sort chunks in a fragmented disk image.

#### Data

This is the actual file data being written.
If there is less file data than fits into the heder, it must be padded.
The value of the padding is left unspecified.

## Empty files

Empty files are special cases.
Encoding an empty file as REFIS results in a REFIS file consisting of a lone master header.
These files should decode back to an empty file.
