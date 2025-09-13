#include "huffman.h"
#include <iostream>
#include <fstream>
#include <filesystem>
#include <bitset>

bool HuffmanCompressor::compress(const std::string& inputFile, const std::string& outputFile) {
    if (!fileExists(inputFile)) {
        std::cerr << "Error: Input file '" << inputFile << "' does not exist.\n";
        return false;
    }
    
    FrequencyTable frequencies = buildFrequencyTable(inputFile);
    if (frequencies.empty()) {
        std::cerr << "Error: Input file is empty or cannot be read.\n";
        return false;
    }
    
    if (frequencies.size() == 1) {
        auto it = frequencies.begin();
        std::ofstream output(outputFile, std::ios::binary);
        if (!output.is_open()) {
            std::cerr << "Error: Cannot create output file '" << outputFile << "'.\n";
            return false;
        }
        
        uint32_t fileSize = static_cast<uint32_t>(getFileSize(inputFile));
        output.write(reinterpret_cast<const char*>(&fileSize), sizeof(fileSize));
        output.write(reinterpret_cast<const char*>(&it->first), sizeof(it->first));
        output.close();
        
        std::cout << "Compression completed: " << inputFile << " -> " << outputFile << "\n";
        std::cout << "Original size: " << getFileSize(inputFile) << " bytes\n";
        std::cout << "Compressed size: " << getFileSize(outputFile) << " bytes\n";
        return true;
    }
    
    HuffmanTree root = buildHuffmanTree(frequencies);
    CodeTable codeTable;
    generateCodes(root, "", codeTable);
    
    writeCompressedFile(inputFile, outputFile, root, codeTable);
    
    std::cout << "Compression completed: " << inputFile << " -> " << outputFile << "\n";
    std::cout << "Original size: " << getFileSize(inputFile) << " bytes\n";
    std::cout << "Compressed size: " << getFileSize(outputFile) << " bytes\n";
    
    return true;
}

bool HuffmanCompressor::decompress(const std::string& inputFile, const std::string& outputFile) {
    if (!fileExists(inputFile)) {
        std::cerr << "Error: Input file '" << inputFile << "' does not exist.\n";
        return false;
    }
    
    bool success = readCompressedFile(inputFile, outputFile);
    
    if (success) {
        std::cout << "Decompression completed: " << inputFile << " -> " << outputFile << "\n";
        std::cout << "Compressed size: " << getFileSize(inputFile) << " bytes\n";
        std::cout << "Decompressed size: " << getFileSize(outputFile) << " bytes\n";
    }
    
    return success;
}

bool HuffmanCompressor::isValidHuffmanFile(const std::string& filename) {
    if (!fileExists(filename)) {
        return false;
    }
    
    std::ifstream file(filename, std::ios::binary);
    if (!file.is_open()) {
        return false;
    }
    
    size_t fileSize = getFileSize(filename);
    if (fileSize < sizeof(uint32_t)) {
        file.close();
        return false;
    }
    
    file.close();
    return true;
}

HuffmanCompressor::FrequencyTable HuffmanCompressor::buildFrequencyTable(const std::string& filename) {
    FrequencyTable frequencies;
    std::ifstream file(filename, std::ios::binary);
    
    if (!file.is_open()) {
        return frequencies;
    }
    
    unsigned char ch;
    while (file.read(reinterpret_cast<char*>(&ch), 1)) {
        frequencies[ch]++;
    }
    
    file.close();
    return frequencies;
}

HuffmanCompressor::HuffmanTree HuffmanCompressor::buildHuffmanTree(const FrequencyTable& frequencies) {
    PriorityQueue pq;
    
    for (const auto& pair : frequencies) {
        pq.push(std::make_shared<HuffmanNode>(pair.first, pair.second));
    }
    
    while (pq.size() > 1) {
        HuffmanTree right = pq.top(); pq.pop();
        HuffmanTree left = pq.top(); pq.pop();
        
        HuffmanTree merged = std::make_shared<HuffmanNode>(
            left->frequency + right->frequency, left, right);
        
        pq.push(merged);
    }
    
    return pq.top();
}

void HuffmanCompressor::generateCodes(const HuffmanTree& root, const std::string& code, CodeTable& codeTable) {
    if (!root) return;
    
    if (root->isLeaf()) {
        codeTable[root->character] = code.empty() ? "0" : code;
        return;
    }
    
    generateCodes(root->left, code + "0", codeTable);
    generateCodes(root->right, code + "1", codeTable);
}

void HuffmanCompressor::serializeTree(const HuffmanTree& root, std::vector<bool>& serialized) {
    if (!root) return;
    
    if (root->isLeaf()) {
        serialized.push_back(true);
        for (int i = 7; i >= 0; i--) {
            serialized.push_back((root->character >> i) & 1);
        }
    } else {
        serialized.push_back(false);
        serializeTree(root->left, serialized);
        serializeTree(root->right, serialized);
    }
}

HuffmanCompressor::HuffmanTree HuffmanCompressor::deserializeTree(const std::vector<bool>& serialized, size_t& index) {
    if (index >= serialized.size()) return nullptr;
    
    if (serialized[index++]) {
        unsigned char ch = 0;
        for (int i = 0; i < 8; i++) {
            if (index >= serialized.size()) return nullptr;
            ch = (ch << 1) | serialized[index++];
        }
        return std::make_shared<HuffmanNode>(ch, 0);
    } else {
        HuffmanTree left = deserializeTree(serialized, index);
        HuffmanTree right = deserializeTree(serialized, index);
        return std::make_shared<HuffmanNode>(0, left, right);
    }
}

void HuffmanCompressor::writeCompressedFile(const std::string& inputFile, const std::string& outputFile, 
                                          const HuffmanTree& root, const CodeTable& codeTable) {
    std::ifstream input(inputFile, std::ios::binary);
    std::ofstream output(outputFile, std::ios::binary);
    
    if (!input.is_open() || !output.is_open()) {
        std::cerr << "Error: Cannot open files for compression.\n";
        return;
    }
    
    uint32_t originalSize = static_cast<uint32_t>(getFileSize(inputFile));
    output.write(reinterpret_cast<const char*>(&originalSize), sizeof(originalSize));
    
    std::vector<bool> serializedTree;
    serializeTree(root, serializedTree);
    
    uint32_t treeSize = static_cast<uint32_t>(serializedTree.size());
    output.write(reinterpret_cast<const char*>(&treeSize), sizeof(treeSize));
    
    std::vector<unsigned char> treeBytes;
    for (size_t i = 0; i < serializedTree.size(); i += 8) {
        unsigned char byte = 0;
        for (int j = 0; j < 8 && i + j < serializedTree.size(); j++) {
            byte = (byte << 1) | serializedTree[i + j];
        }
        if (i + 8 > serializedTree.size()) {
            byte <<= (8 - (serializedTree.size() % 8));
        }
        treeBytes.push_back(byte);
    }
    
    output.write(reinterpret_cast<const char*>(treeBytes.data()), treeBytes.size());
    
    std::string encodedData;
    unsigned char ch;
    while (input.read(reinterpret_cast<char*>(&ch), 1)) {
        encodedData += codeTable.at(ch);
    }
    
    uint32_t encodedBits = static_cast<uint32_t>(encodedData.length());
    output.write(reinterpret_cast<const char*>(&encodedBits), sizeof(encodedBits));
    
    for (size_t i = 0; i < encodedData.length(); i += 8) {
        unsigned char byte = 0;
        for (int j = 0; j < 8 && i + j < encodedData.length(); j++) {
            byte = (byte << 1) | (encodedData[i + j] == '1' ? 1 : 0);
        }
        if (i + 8 > encodedData.length()) {
            byte <<= (8 - (encodedData.length() % 8));
        }
        output.write(reinterpret_cast<const char*>(&byte), 1);
    }
    
    input.close();
    output.close();
}

bool HuffmanCompressor::readCompressedFile(const std::string& inputFile, const std::string& outputFile) {
    std::ifstream input(inputFile, std::ios::binary);
    std::ofstream output(outputFile, std::ios::binary);
    
    if (!input.is_open() || !output.is_open()) {
        std::cerr << "Error: Cannot open files for decompression.\n";
        return false;
    }
    
    uint32_t originalSize;
    input.read(reinterpret_cast<char*>(&originalSize), sizeof(originalSize));
    
    if (originalSize == 0) {
        input.close();
        output.close();
        return true;
    }
    
    uint32_t treeSize;
    if (!input.read(reinterpret_cast<char*>(&treeSize), sizeof(treeSize))) {
        unsigned char singleChar;
        input.seekg(sizeof(uint32_t));
        input.read(reinterpret_cast<char*>(&singleChar), sizeof(singleChar));
        
        for (uint32_t i = 0; i < originalSize; i++) {
            output.write(reinterpret_cast<const char*>(&singleChar), 1);
        }
        
        input.close();
        output.close();
        return true;
    }
    
    std::vector<unsigned char> treeBytes((treeSize + 7) / 8);
    input.read(reinterpret_cast<char*>(treeBytes.data()), treeBytes.size());
    
    std::vector<bool> serializedTree;
    for (size_t i = 0; i < treeBytes.size(); i++) {
        for (int j = 7; j >= 0; j--) {
            if (serializedTree.size() < treeSize) {
                serializedTree.push_back((treeBytes[i] >> j) & 1);
            }
        }
    }
    
    size_t index = 0;
    HuffmanTree root = deserializeTree(serializedTree, index);
    
    uint32_t encodedBits;
    input.read(reinterpret_cast<char*>(&encodedBits), sizeof(encodedBits));
    
    std::vector<unsigned char> encodedBytes((encodedBits + 7) / 8);
    input.read(reinterpret_cast<char*>(encodedBytes.data()), encodedBytes.size());
    
    std::string bitString;
    for (size_t i = 0; i < encodedBytes.size(); i++) {
        for (int j = 7; j >= 0; j--) {
            if (bitString.length() < encodedBits) {
                bitString += ((encodedBytes[i] >> j) & 1) ? '1' : '0';
            }
        }
    }
    
    HuffmanTree current = root;
    for (char bit : bitString) {
        if (bit == '0') {
            current = current->left;
        } else {
            current = current->right;
        }
        
        if (current->isLeaf()) {
            output.write(reinterpret_cast<const char*>(&current->character), 1);
            current = root;
        }
    }
    
    input.close();
    output.close();
    return true;
}

bool HuffmanCompressor::fileExists(const std::string& filename) {
    return std::filesystem::exists(filename);
}

size_t HuffmanCompressor::getFileSize(const std::string& filename) {
    try {
        return std::filesystem::file_size(filename);
    } catch (const std::filesystem::filesystem_error&) {
        return 0;
    }
}