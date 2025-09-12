#include "rle.h"
#include <iostream>
#include <filesystem>

bool RLECompressor::compress(const std::string& inputFile, const std::string& outputFile) {
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
    
    unsigned char currentChar = 0;
    unsigned char count = 0;
    bool firstChar = true;
    
    unsigned char ch;
    while (input.read(reinterpret_cast<char*>(&ch), 1)) {
        if (firstChar) {
            currentChar = ch;
            count = 1;
            firstChar = false;
        } else if (ch == currentChar && count < MAX_RUN_LENGTH) {
            count++;
        } else {
            writeRunLength(output, count, currentChar);
            currentChar = ch;
            count = 1;
        }
    }
    
    if (!firstChar) {
        writeRunLength(output, count, currentChar);
    }
    
    input.close();
    output.close();
    
    std::cout << "Compression completed: " << inputFile << " -> " << outputFile << "\n";
    std::cout << "Original size: " << getFileSize(inputFile) << " bytes\n";
    std::cout << "Compressed size: " << getFileSize(outputFile) << " bytes\n";
    
    return true;
}

bool RLECompressor::decompress(const std::string& inputFile, const std::string& outputFile) {
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
    
    unsigned char count, character;
    while (readRunLength(input, count, character)) {
        for (unsigned char i = 0; i < count; ++i) {
            output.write(reinterpret_cast<const char*>(&character), 1);
        }
    }
    
    input.close();
    output.close();
    
    std::cout << "Decompression completed: " << inputFile << " -> " << outputFile << "\n";
    std::cout << "Compressed size: " << getFileSize(inputFile) << " bytes\n";
    std::cout << "Decompressed size: " << getFileSize(outputFile) << " bytes\n";
    
    return true;
}

bool RLECompressor::isValidRLEFile(const std::string& filename) {
    if (!fileExists(filename)) {
        return false;
    }
    
    std::ifstream file(filename, std::ios::binary);
    if (!file.is_open()) {
        return false;
    }
    
    size_t fileSize = getFileSize(filename);
    if (fileSize == 0 || fileSize % 2 != 0) {
        file.close();
        return false;
    }
    
    file.close();
    return true;
}

void RLECompressor::writeRunLength(std::ofstream& output, unsigned char count, unsigned char character) {
    output.write(reinterpret_cast<const char*>(&count), 1);
    output.write(reinterpret_cast<const char*>(&character), 1);
}

bool RLECompressor::readRunLength(std::ifstream& input, unsigned char& count, unsigned char& character) {
    if (input.read(reinterpret_cast<char*>(&count), 1) &&
        input.read(reinterpret_cast<char*>(&character), 1)) {
        return true;
    }
    return false;
}

bool RLECompressor::fileExists(const std::string& filename) {
    return std::filesystem::exists(filename);
}

size_t RLECompressor::getFileSize(const std::string& filename) {
    try {
        return std::filesystem::file_size(filename);
    } catch (const std::filesystem::filesystem_error&) {
        return 0;
    }
}