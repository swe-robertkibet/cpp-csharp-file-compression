#pragma once

#include <string>
#include <fstream>
#include <vector>

class RLECompressor {
public:
    static bool compress(const std::string& inputFile, const std::string& outputFile);
    
    static bool decompress(const std::string& inputFile, const std::string& outputFile);
    
    static bool isValidRLEFile(const std::string& filename);

private:
    static constexpr unsigned char MAX_RUN_LENGTH = 255;
    
    static void writeRunLength(std::ofstream& output, unsigned char count, unsigned char character);
    
    static bool readRunLength(std::ifstream& input, unsigned char& count, unsigned char& character);
    
    static bool fileExists(const std::string& filename);
    
    static size_t getFileSize(const std::string& filename);
};