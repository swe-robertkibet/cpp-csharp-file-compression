#include "lzw.h"
#include <iostream>
#include <filesystem>

BitWriter::BitWriter(std::ofstream& output) : output_(output), buffer_(0), bitsInBuffer_(0) {}

BitWriter::~BitWriter() {
    flush();
}

void BitWriter::writeBits(uint32_t value, int numBits) {
    while (numBits > 0) {
        int bitsToWrite = std::min(numBits, 32 - bitsInBuffer_);
        buffer_ |= (value & ((1u << bitsToWrite) - 1)) << (32 - bitsInBuffer_ - bitsToWrite);
        bitsInBuffer_ += bitsToWrite;
        numBits -= bitsToWrite;
        value >>= bitsToWrite;
        
        if (bitsInBuffer_ == 32) {
            uint8_t bytes[4];
            bytes[0] = (buffer_ >> 24) & 0xFF;
            bytes[1] = (buffer_ >> 16) & 0xFF;
            bytes[2] = (buffer_ >> 8) & 0xFF;
            bytes[3] = buffer_ & 0xFF;
            output_.write(reinterpret_cast<const char*>(bytes), 4);
            buffer_ = 0;
            bitsInBuffer_ = 0;
        }
    }
}

void BitWriter::flush() {
    if (bitsInBuffer_ > 0) {
        int bytesToWrite = (bitsInBuffer_ + 7) / 8;
        for (int i = 0; i < bytesToWrite; i++) {
            uint8_t byte = (buffer_ >> (24 - i * 8)) & 0xFF;
            output_.write(reinterpret_cast<const char*>(&byte), 1);
        }
        buffer_ = 0;
        bitsInBuffer_ = 0;
    }
}

BitReader::BitReader(std::ifstream& input) : input_(input), buffer_(0), bitsInBuffer_(0), endOfFile_(false) {}

uint32_t BitReader::readBits(int numBits) {
    uint32_t result = 0;
    int bitsRead = 0;
    
    while (bitsRead < numBits && !endOfFile_) {
        if (bitsInBuffer_ == 0) {
            fillBuffer();
        }
        
        if (bitsInBuffer_ > 0) {
            int bitsToRead = std::min(numBits - bitsRead, bitsInBuffer_);
            uint32_t mask = (1u << bitsToRead) - 1;
            uint32_t bits = (buffer_ >> (32 - bitsToRead)) & mask;
            result |= bits << (numBits - bitsRead - bitsToRead);
            buffer_ <<= bitsToRead;
            bitsInBuffer_ -= bitsToRead;
            bitsRead += bitsToRead;
        }
    }
    
    return result;
}

bool BitReader::hasData() const {
    return !endOfFile_ || bitsInBuffer_ > 0;
}

void BitReader::fillBuffer() {
    uint8_t bytes[4];
    input_.read(reinterpret_cast<char*>(bytes), 4);
    int bytesRead = input_.gcount();
    
    if (bytesRead == 0) {
        endOfFile_ = true;
        return;
    }
    
    buffer_ = 0;
    bitsInBuffer_ = bytesRead * 8;
    
    for (int i = 0; i < bytesRead; i++) {
        buffer_ |= static_cast<uint32_t>(bytes[i]) << (24 - i * 8);
    }
    
    if (bytesRead < 4) {
        endOfFile_ = true;
    }
}

bool LZWCompressor::compress(const std::string& inputFile, const std::string& outputFile) {
    if (!fileExists(inputFile)) {
        std::cerr << "Error: Input file '" << inputFile << "' does not exist.\n";
        return false;
    }
    
    std::ifstream input(inputFile, std::ios::binary);
    if (!input.is_open()) {
        std::cerr << "Error: Cannot open input file '" << inputFile << "'.\n";
        return false;
    }
    
    std::ofstream output(outputFile, std::ios::binary);
    if (!output.is_open()) {
        std::cerr << "Error: Cannot create output file '" << outputFile << "'.\n";
        input.close();
        return false;
    }
    
    BitWriter writer(output);
    bool success = compressData(input, writer);
    
    input.close();
    output.close();
    
    if (success) {
        std::cout << "Compression completed: " << inputFile << " -> " << outputFile << "\n";
        std::cout << "Original size: " << getFileSize(inputFile) << " bytes\n";
        std::cout << "Compressed size: " << getFileSize(outputFile) << " bytes\n";
    }
    
    return success;
}

bool LZWCompressor::decompress(const std::string& inputFile, const std::string& outputFile) {
    if (!fileExists(inputFile)) {
        std::cerr << "Error: Input file '" << inputFile << "' does not exist.\n";
        return false;
    }
    
    std::ifstream input(inputFile, std::ios::binary);
    if (!input.is_open()) {
        std::cerr << "Error: Cannot open input file '" << inputFile << "'.\n";
        return false;
    }
    
    std::ofstream output(outputFile, std::ios::binary);
    if (!output.is_open()) {
        std::cerr << "Error: Cannot create output file '" << outputFile << "'.\n";
        input.close();
        return false;
    }
    
    BitReader reader(input);
    bool success = decompressData(reader, output);
    
    input.close();
    output.close();
    
    if (success) {
        std::cout << "Decompression completed: " << inputFile << " -> " << outputFile << "\n";
        std::cout << "Compressed size: " << getFileSize(inputFile) << " bytes\n";
        std::cout << "Decompressed size: " << getFileSize(outputFile) << " bytes\n";
    }
    
    return success;
}

bool LZWCompressor::isValidLZWFile(const std::string& filename) {
    if (!fileExists(filename)) {
        return false;
    }
    
    size_t fileSize = getFileSize(filename);
    return fileSize > 0;
}

LZWCompressor::CompressionDictionary LZWCompressor::buildCompressionDictionary() {
    CompressionDictionary dict;
    
    for (uint16_t i = 0; i < 256; i++) {
        dict[std::string(1, static_cast<char>(i))] = i;
    }
    
    return dict;
}

LZWCompressor::DecompressionDictionary LZWCompressor::buildDecompressionDictionary() {
    DecompressionDictionary dict;
    dict.reserve(MAX_DICTIONARY_SIZE);
    
    for (uint16_t i = 0; i < 256; i++) {
        dict.push_back(std::string(1, static_cast<char>(i)));
    }
    
    dict.push_back("");
    dict.push_back("");
    
    return dict;
}

bool LZWCompressor::compressData(std::ifstream& input, BitWriter& writer) {
    CompressionDictionary dict = buildCompressionDictionary();
    uint16_t nextCode = FIRST_CODE;
    uint16_t codeWidth = INITIAL_CODE_WIDTH;
    
    std::string current;
    char ch;
    
    while (input.read(&ch, 1)) {
        std::string next = current + ch;
        
        if (dict.find(next) != dict.end()) {
            current = next;
        } else {
            writer.writeBits(dict[current], codeWidth);
            
            if (nextCode < MAX_DICTIONARY_SIZE) {
                dict[next] = nextCode++;
                
                if (nextCode > (1u << codeWidth) && codeWidth < MAX_CODE_WIDTH) {
                    codeWidth++;
                }
            } else {
                writer.writeBits(CLEAR_CODE, codeWidth);
                dict = buildCompressionDictionary();
                nextCode = FIRST_CODE;
                codeWidth = INITIAL_CODE_WIDTH;
            }
            
            current = ch;
        }
    }
    
    if (!current.empty()) {
        writer.writeBits(dict[current], codeWidth);
    }
    
    writer.writeBits(STOP_CODE, codeWidth);
    return true;
}

bool LZWCompressor::decompressData(BitReader& reader, std::ofstream& output) {
    DecompressionDictionary dict = buildDecompressionDictionary();
    uint16_t nextCode = FIRST_CODE;
    uint16_t codeWidth = INITIAL_CODE_WIDTH;
    
    uint16_t prevCode = reader.readBits(codeWidth);
    if (prevCode == STOP_CODE) {
        return true;
    }
    
    if (prevCode >= dict.size()) {
        std::cerr << "Error: Invalid LZW code in compressed data.\n";
        return false;
    }
    
    std::string prevString = dict[prevCode];
    output.write(prevString.c_str(), prevString.length());
    
    while (reader.hasData()) {
        uint16_t code = reader.readBits(codeWidth);
        
        if (code == STOP_CODE) {
            break;
        }
        
        if (code == CLEAR_CODE) {
            dict = buildDecompressionDictionary();
            nextCode = FIRST_CODE;
            codeWidth = INITIAL_CODE_WIDTH;
            
            prevCode = reader.readBits(codeWidth);
            if (prevCode == STOP_CODE) {
                break;
            }
            
            prevString = dict[prevCode];
            output.write(prevString.c_str(), prevString.length());
            continue;
        }
        
        std::string currentString;
        if (code < dict.size()) {
            currentString = dict[code];
        } else if (code == nextCode) {
            currentString = prevString + prevString[0];
        } else {
            std::cerr << "Error: Invalid LZW code in compressed data.\n";
            return false;
        }
        
        output.write(currentString.c_str(), currentString.length());
        
        if (nextCode < MAX_DICTIONARY_SIZE) {
            dict.push_back(prevString + currentString[0]);
            nextCode++;
            
            if (nextCode > (1u << codeWidth) && codeWidth < MAX_CODE_WIDTH) {
                codeWidth++;
            }
        }
        
        prevString = currentString;
        prevCode = code;
    }
    
    return true;
}

bool LZWCompressor::fileExists(const std::string& filename) {
    return std::filesystem::exists(filename);
}

size_t LZWCompressor::getFileSize(const std::string& filename) {
    try {
        return std::filesystem::file_size(filename);
    } catch (const std::filesystem::filesystem_error&) {
        return 0;
    }
}