#include <iostream>
#include <string>
#include "rle.h"
#include "huffman.h"
#include "cxxopts.hpp"

int main(int argc, char* argv[]) {
    cxxopts::Options options("compress", "Multi-Algorithm Compression Tool");
    
    options.add_options()
        ("algo", "Compression algorithm: 'rle' or 'huffman'", cxxopts::value<std::string>())
        ("mode", "Operation mode: 'compress' or 'decompress'", cxxopts::value<std::string>())
        ("input", "Input file path", cxxopts::value<std::string>())
        ("output", "Output file path", cxxopts::value<std::string>())
        ("h,help", "Show help information");
    
    try {
        auto result = options.parse(argc, argv);
        
        if (result.count("help")) {
            std::cout << options.help() << std::endl;
            std::cout << "\nExample usage:" << std::endl;
            std::cout << "  ./compress --algo rle --mode compress --input sample.txt --output sample.rle" << std::endl;
            std::cout << "  ./compress --algo rle --mode decompress --input sample.rle --output restored.txt" << std::endl;
            std::cout << "  ./compress --algo huffman --mode compress --input sample.txt --output sample.huf" << std::endl;
            std::cout << "  ./compress --algo huffman --mode decompress --input sample.huf --output restored.txt" << std::endl;
            return 0;
        }
        
        if (!result.count("algo")) {
            std::cerr << "Error: --algo parameter is required" << std::endl;
            return 1;
        }
        
        if (!result.count("mode")) {
            std::cerr << "Error: --mode parameter is required" << std::endl;
            return 1;
        }
        
        if (!result.count("input")) {
            std::cerr << "Error: --input parameter is required" << std::endl;
            return 1;
        }
        
        if (!result.count("output")) {
            std::cerr << "Error: --output parameter is required" << std::endl;
            return 1;
        }
        
        std::string algorithm = result["algo"].as<std::string>();
        std::string mode = result["mode"].as<std::string>();
        std::string inputFile = result["input"].as<std::string>();
        std::string outputFile = result["output"].as<std::string>();
        
        if (algorithm != "rle" && algorithm != "huffman") {
            std::cerr << "Error: Supported algorithms are 'rle' and 'huffman'" << std::endl;
            return 1;
        }
        
        if (mode != "compress" && mode != "decompress") {
            std::cerr << "Error: Mode must be either 'compress' or 'decompress'" << std::endl;
            return 1;
        }
        
        if (inputFile == outputFile) {
            std::cerr << "Error: Input and output files cannot be the same" << std::endl;
            return 1;
        }
        
        std::cout << "Multi-Algorithm Compression Tool" << std::endl;
        std::cout << "Algorithm: " << algorithm << std::endl;
        std::cout << "Mode: " << mode << std::endl;
        std::cout << "Input: " << inputFile << std::endl;
        std::cout << "Output: " << outputFile << std::endl;
        std::cout << "---" << std::endl;
        
        bool success = false;
        
        if (mode == "compress") {
            success = RLECompressor::compress(inputFile, outputFile);
        } else if (mode == "decompress") {
            if (!RLECompressor::isValidRLEFile(inputFile)) {
                std::cerr << "Warning: Input file may not be a valid RLE compressed file" << std::endl;
            }
            success = RLECompressor::decompress(inputFile, outputFile);
        }
        
        if (success) {
            std::cout << "Operation completed successfully!" << std::endl;
            return 0;
        } else {
            std::cerr << "Operation failed!" << std::endl;
            return 1;
        }
        
    } catch (const cxxopts::exceptions::exception& e) {
        std::cerr << "Error parsing arguments: " << e.what() << std::endl;
        std::cout << options.help() << std::endl;
        return 1;
    } catch (const std::exception& e) {
        std::cerr << "Unexpected error: " << e.what() << std::endl;
        return 1;
    }
}