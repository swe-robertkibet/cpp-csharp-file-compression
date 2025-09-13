#pragma once

#include <string>
#include <unordered_map>
#include <vector>
#include <fstream>
#include <cstdint>

class BitWriter {
public:
    BitWriter(std::ofstream& output);
    ~BitWriter();
    
    void writeBits(uint32_t value, int numBits);
    void flush();

private:
    std::ofstream& output_;
    uint32_t buffer_;
    int bitsInBuffer_;
};

class BitReader {
public:
    BitReader(std::ifstream& input);
    
    uint32_t readBits(int numBits);
    bool hasData() const;

private:
    std::ifstream& input_;
    uint32_t buffer_;
    int bitsInBuffer_;
    bool endOfFile_;
    
    void fillBuffer();
};

class LZWCompressor {
public:
    static bool compress(const std::string& inputFile, const std::string& outputFile);
    
    static bool decompress(const std::string& inputFile, const std::string& outputFile);
    
    static bool isValidLZWFile(const std::string& filename);

private:
    static constexpr uint16_t INITIAL_CODE_WIDTH = 9;
    static constexpr uint16_t MAX_CODE_WIDTH = 15;
    static constexpr uint16_t MAX_DICTIONARY_SIZE = (1 << MAX_CODE_WIDTH);
    static constexpr uint16_t CLEAR_CODE = 256;
    static constexpr uint16_t STOP_CODE = 257;
    static constexpr uint16_t FIRST_CODE = 258;
    
    using CompressionDictionary = std::unordered_map<std::string, uint16_t>;
    using DecompressionDictionary = std::vector<std::string>;
    
    static CompressionDictionary buildCompressionDictionary();
    
    static DecompressionDictionary buildDecompressionDictionary();
    
    static bool compressData(std::ifstream& input, BitWriter& writer);
    
    static bool decompressData(BitReader& reader, std::ofstream& output);
    
    static bool fileExists(const std::string& filename);
    
    static size_t getFileSize(const std::string& filename);
};