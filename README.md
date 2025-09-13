# Multi-Algorithm Compression Tool

A C++ implementation of three fundamental compression algorithms: **Run-Length Encoding (RLE)**, **Huffman Coding**, and **LZW (Lempel-Ziv-Welch)**. This tool demonstrates the strengths and weaknesses of each algorithm across different data types.

suggested pseudocode for RLE compression

```cpp
function compressRLE(inputFile, outputFile):
    open inputFile for reading
    open outputFile for writing
    currentChar = None
    count = 0

    for char in inputFile:
        if char == currentChar:
            count += 1
        else:
            if currentChar is not None:
                write (count, currentChar) to outputFile
            currentChar = char
            count = 1

    write (count, currentChar) to outputFile
    close files

function decompressRLE(inputFile, outputFile):
    open inputFile for reading
    open outputFile for writing

    while not end of inputFile:
        read count, char
        write char repeated count times to outputFile

    close files
```

suggested psudocode for huffman coding

```cpp
function buildFrequencyTable(inputFile):
    freq = empty map
    for char in inputFile:
        freq[char] += 1
    return freq

function buildHuffmanTree(freq):
    create priority queue pq
    for each char, count in freq:
        create node and push to pq

    while pq.size > 1:
        left = pq.pop()
        right = pq.pop()
        newNode = Node(left + right)  # sum of frequencies
        pq.push(newNode)

    return pq.pop()  # root node

function generateCodes(node, prefix="", codeMap={}):
    if node is leaf:
        codeMap[node.char] = prefix
    else:
        generateCodes(node.left, prefix + "0", codeMap)
        generateCodes(node.right, prefix + "1", codeMap)
    return codeMap

function compressHuffman(inputFile, outputFile):
    freq = buildFrequencyTable(inputFile)
    root = buildHuffmanTree(freq)
    codeMap = generateCodes(root)
    write codeMap to outputFile
    for char in inputFile:
        write codeMap[char] bits to outputFile

function decompressHuffman(inputFile, outputFile):
    read codeMap from inputFile
    currentBits = ""
    while not end of inputFile:
        currentBits += read next bit
        if currentBits in codeMap:
            write codeMap[currentBits] to outputFile
            currentBits = ""

```

suggested psudocode for LZW (Lempel–Ziv–Welch)

```cpp
function compressLZW(inputFile, outputFile):
    dict = initialize dictionary with all single characters
    string = ""
    for char in inputFile:
        stringPlusChar = string + char
        if stringPlusChar in dict:
            string = stringPlusChar
        else:
            write dict[string] to outputFile
            add stringPlusChar to dict with next available code
            string = char
    write dict[string] to outputFile

function decompressLZW(inputFile, outputFile):
    dict = initialize dictionary with all single characters
    prevCode = read next code from inputFile
    write dict[prevCode] to outputFile

    for code in inputFile:
        if code in dict:
            entry = dict[code]
        else:
            entry = dict[prevCode] + first character of dict[prevCode]
        write entry to outputFile
        add dict[prevCode] + first character of entry to dict
        prevCode = code

```

## Quick Start

```bash
# Build the project
mkdir build && cd build
cmake .. && make

# Compress a file with RLE
./compress --algo rle --mode compress --input data.txt --output data.rle

# Decompress with RLE
./compress --algo rle --mode decompress --input data.rle --output restored.txt

# Try other algorithms: huffman, lzw
./compress --algo huffman --mode compress --input data.txt --output data.huf
./compress --algo lzw --mode compress --input data.txt --output data.lzw
```

## 📊 Algorithm Comparison

Testing across 8 different file types reveals each algorithm's optimal use cases:

| File Type                                  | Size      | RLE Result         | Huffman Result   | LZW Result       | Winner  |
| ------------------------------------------ | --------- | ------------------ | ---------------- | ---------------- | ------- |
| **Empty file**                             | 0 bytes   | 0 bytes (✓)        | ERROR (✗)        | 2 bytes          | RLE     |
| **Single char** ("a")                      | 1 byte    | 2 bytes (200%)     | 5 bytes (500%)   | 3 bytes (300%)   | RLE     |
| **Simple text** ("hello world hello")      | 17 bytes  | 30 bytes (176%)    | 28 bytes (165%)  | 18 bytes (106%)  | **LZW** |
| **Repetitive pattern** ("aaabbbccc...zzz") | 78 bytes  | 52 bytes (67%)     | 92 bytes (118%)  | 60 bytes (77%)   | **RLE** |
| **Long runs** (318 × 'a')                  | 318 bytes | **4 bytes (1.3%)** | 5 bytes (1.6%)   | 30 bytes (9.4%)  | **RLE** |
| **Random text** (mixed content)            | 228 bytes | 452 bytes (198%)   | 272 bytes (119%) | 234 bytes (103%) | **LZW** |
| **Mixed patterns** (repetitive + random)   | 260 bytes | 334 bytes (128%)   | 245 bytes (94%)  | 195 bytes (75%)  | **LZW** |
| **Binary-like** (hex patterns)             | 206 bytes | 348 bytes (169%)   | 162 bytes (79%)  | 150 bytes (73%)  | **LZW** |

### Performance Summary

| Algorithm   | Best Compression     | Worst Case           | Reliability       | Use Case               |
| ----------- | -------------------- | -------------------- | ----------------- | ---------------------- |
| **RLE**     | 98.7% (long runs)    | 198% expansion       | Inconsistent      | Specific patterns only |
| **Huffman** | 98.4% (uniform data) | Fails on empty files | Moderate          | Frequency-based text   |
| **LZW**     | 90.6% (repetitive)   | 106% expansion       | **Most reliable** | **General purpose**    |

## Algorithm Selection Guide

### Choose **RLE** when:

- Data has **long runs** of identical characters (images, simple graphics)
- **Simple implementation** is prioritized
- **Avoid** for diverse/random content (can double file size!)

### Choose **Huffman** when:

- Text has **uneven character frequencies**
- File size is **reasonably large** (>100 bytes)
- Need **optimal compression** for specific probability distributions
- **Avoid** for small files or empty files

### Choose **LZW** when:

- **General-purpose compression** needed
- Data contains **repeating patterns** or sequences
- Want **consistent performance** across different data types
- Need **reliable compression** without dramatic expansion

## Technical Implementation

### RLE (Run-Length Encoding)

- **Format**: (count, character) pairs
- **Best for**: Long runs of identical values
- **Compression ratio**: 1.3% - 200% (highly variable)

### Huffman Coding

- **Format**: Frequency table + variable-length bit codes
- **Best for**: Text with uneven character frequencies
- **Compression ratio**: 79% - 500% (depends on entropy)

### LZW (Lempel-Ziv-Welch)

- **Format**: Variable-width codes (9-15 bits) with dynamic dictionary
- **Best for**: Data with repeating sequences
- **Compression ratio**: 73% - 106% (most consistent)
- **Fixed bug**: RAII scope issue where BitWriter::flush() was called after file close

## Testing

The tool includes comprehensive test coverage across multiple data patterns:

```bash
# Run all algorithm tests
cd build
./compress --algo rle --mode compress --input ../tests/sample.txt --output sample.rle
./compress --algo huffman --mode compress --input ../tests/sample.txt --output sample.huf
./compress --algo lzw --mode compress --input ../tests/sample.txt --output sample.lzw

# Verify round-trip integrity
./compress --algo lzw --mode decompress --input sample.lzw --output restored.txt
diff ../tests/sample.txt restored.txt  # Should show no differences
```

## Build Requirements

- **C++17** or later
- **CMake 3.10+**
- **GCC/Clang** with standard library support

```bash
mkdir build && cd build
cmake ..
make
```

## 📈 Benchmark Results

Based on extensive testing, **LZW emerges as the most reliable general-purpose algorithm**, providing:

- Consistent performance across data types
- Good compression on pattern-rich data
- Minimal expansion on random data
- No catastrophic failures

**RLE excels in specialized scenarios** with long character runs, while **Huffman works best for text with known frequency distributions**.

---
