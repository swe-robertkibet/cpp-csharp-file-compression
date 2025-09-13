#pragma once

#include <string>
#include <unordered_map>
#include <priority_queue>
#include <vector>
#include <memory>

struct HuffmanNode {
    unsigned char character;
    int frequency;
    std::shared_ptr<HuffmanNode> left;
    std::shared_ptr<HuffmanNode> right;
    
    HuffmanNode(unsigned char ch, int freq) 
        : character(ch), frequency(freq), left(nullptr), right(nullptr) {}
    
    HuffmanNode(int freq, std::shared_ptr<HuffmanNode> l, std::shared_ptr<HuffmanNode> r)
        : character(0), frequency(freq), left(l), right(r) {}
    
    bool isLeaf() const {
        return left == nullptr && right == nullptr;
    }
};

struct HuffmanNodeComparator {
    bool operator()(const std::shared_ptr<HuffmanNode>& a, const std::shared_ptr<HuffmanNode>& b) {
        if (a->frequency == b->frequency) {
            return a->character > b->character;
        }
        return a->frequency > b->frequency;
    }
};

class HuffmanCompressor {
public:
    static bool compress(const std::string& inputFile, const std::string& outputFile);
    
    static bool decompress(const std::string& inputFile, const std::string& outputFile);
    
    static bool isValidHuffmanFile(const std::string& filename);

private:
    using HuffmanTree = std::shared_ptr<HuffmanNode>;
    using FrequencyTable = std::unordered_map<unsigned char, int>;
    using CodeTable = std::unordered_map<unsigned char, std::string>;
    using PriorityQueue = std::priority_queue<HuffmanTree, std::vector<HuffmanTree>, HuffmanNodeComparator>;
    
    static FrequencyTable buildFrequencyTable(const std::string& filename);
    
    static HuffmanTree buildHuffmanTree(const FrequencyTable& frequencies);
    
    static void generateCodes(const HuffmanTree& root, const std::string& code, CodeTable& codeTable);
    
    static void serializeTree(const HuffmanTree& root, std::vector<bool>& serialized);
    
    static HuffmanTree deserializeTree(const std::vector<bool>& serialized, size_t& index);
    
    static void writeCompressedFile(const std::string& inputFile, const std::string& outputFile, 
                                  const HuffmanTree& root, const CodeTable& codeTable);
    
    static bool readCompressedFile(const std::string& inputFile, const std::string& outputFile);
    
    static bool fileExists(const std::string& filename);
    
    static size_t getFileSize(const std::string& filename);
};