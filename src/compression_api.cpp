#include "compression_api.h"
#include "rle.h"
#include "huffman.h"
#include "lzw.h"
#include <chrono>
#include <cstring>
#include <filesystem>
#include <string>

static thread_local char last_error[256] = {0};

void set_error(const char* message) {
    strncpy(last_error, message, sizeof(last_error) - 1);
    last_error[sizeof(last_error) - 1] = '\0';
}

uint64_t get_file_size_internal(const std::string& filename) {
    try {
        return std::filesystem::file_size(filename);
    } catch (...) {
        return 0;
    }
}

double calculate_speed_mbps(uint64_t bytes, double time_ms) {
    if (time_ms <= 0) return 0.0;
    double seconds = time_ms / 1000.0;
    double megabytes = static_cast<double>(bytes) / (1024.0 * 1024.0);
    return megabytes / seconds;
}

int compress_file(CompressionAlgorithm algorithm, const char* input_file, const char* output_file, CompressionMetrics* metrics) {
    if (!input_file || !output_file || !metrics) {
        set_error("Invalid parameters");
        return 0;
    }

    memset(metrics, 0, sizeof(CompressionMetrics));
    strcpy(metrics->error_message, "");

    std::string input_str(input_file);
    std::string output_str(output_file);

    metrics->original_size_bytes = get_file_size_internal(input_str);
    if (metrics->original_size_bytes == 0 && !std::filesystem::exists(input_str)) {
        strcpy(metrics->error_message, "Input file does not exist");
        return 0;
    }

    auto start_time = std::chrono::high_resolution_clock::now();
    bool success = false;

    try {
        switch (algorithm) {
            case ALGORITHM_RLE:
                success = RLECompressor::compress(input_str, output_str);
                break;
            case ALGORITHM_HUFFMAN:
                success = HuffmanCompressor::compress(input_str, output_str);
                break;
            case ALGORITHM_LZW:
                success = LZWCompressor::compress(input_str, output_str);
                break;
            default:
                strcpy(metrics->error_message, "Invalid algorithm");
                return 0;
        }
    } catch (const std::exception& e) {
        strcpy(metrics->error_message, e.what());
        return 0;
    } catch (...) {
        strcpy(metrics->error_message, "Unknown error during compression");
        return 0;
    }

    auto end_time = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end_time - start_time);
    metrics->compression_time_ms = duration.count() / 1000.0;

    if (success) {
        metrics->compressed_size_bytes = get_file_size_internal(output_str);

        if (metrics->original_size_bytes > 0) {
            metrics->compression_ratio = (static_cast<double>(metrics->compressed_size_bytes) /
                                        static_cast<double>(metrics->original_size_bytes)) * 100.0;
        }

        metrics->compression_speed_mbps = calculate_speed_mbps(metrics->original_size_bytes,
                                                              metrics->compression_time_ms);
        metrics->success = 1;
    } else {
        strcpy(metrics->error_message, "Compression failed");
        metrics->success = 0;
    }

    return success ? 1 : 0;
}

int decompress_file(CompressionAlgorithm algorithm, const char* input_file, const char* output_file, CompressionMetrics* metrics) {
    if (!input_file || !output_file || !metrics) {
        set_error("Invalid parameters");
        return 0;
    }

    memset(metrics, 0, sizeof(CompressionMetrics));
    strcpy(metrics->error_message, "");

    std::string input_str(input_file);
    std::string output_str(output_file);

    metrics->compressed_size_bytes = get_file_size_internal(input_str);
    if (metrics->compressed_size_bytes == 0 && !std::filesystem::exists(input_str)) {
        strcpy(metrics->error_message, "Input file does not exist");
        return 0;
    }

    auto start_time = std::chrono::high_resolution_clock::now();
    bool success = false;

    try {
        switch (algorithm) {
            case ALGORITHM_RLE:
                success = RLECompressor::decompress(input_str, output_str);
                break;
            case ALGORITHM_HUFFMAN:
                success = HuffmanCompressor::decompress(input_str, output_str);
                break;
            case ALGORITHM_LZW:
                success = LZWCompressor::decompress(input_str, output_str);
                break;
            default:
                strcpy(metrics->error_message, "Invalid algorithm");
                return 0;
        }
    } catch (const std::exception& e) {
        strcpy(metrics->error_message, e.what());
        return 0;
    } catch (...) {
        strcpy(metrics->error_message, "Unknown error during decompression");
        return 0;
    }

    auto end_time = std::chrono::high_resolution_clock::now();
    auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end_time - start_time);
    metrics->decompression_time_ms = duration.count() / 1000.0;

    if (success) {
        metrics->original_size_bytes = get_file_size_internal(output_str);

        if (metrics->original_size_bytes > 0) {
            metrics->compression_ratio = (static_cast<double>(metrics->compressed_size_bytes) /
                                        static_cast<double>(metrics->original_size_bytes)) * 100.0;
        }

        metrics->decompression_speed_mbps = calculate_speed_mbps(metrics->original_size_bytes,
                                                                metrics->decompression_time_ms);
        metrics->success = 1;
    } else {
        strcpy(metrics->error_message, "Decompression failed");
        metrics->success = 0;
    }

    return success ? 1 : 0;
}

int get_file_size(const char* filename, uint64_t* size) {
    if (!filename || !size) {
        set_error("Invalid parameters");
        return 0;
    }

    *size = get_file_size_internal(std::string(filename));
    return *size > 0 || std::filesystem::exists(filename) ? 1 : 0;
}

const char* get_algorithm_name(CompressionAlgorithm algorithm) {
    switch (algorithm) {
        case ALGORITHM_RLE: return "Run-Length Encoding";
        case ALGORITHM_HUFFMAN: return "Huffman Coding";
        case ALGORITHM_LZW: return "LZW";
        default: return "Unknown";
    }
}

const char* get_last_error() {
    return last_error;
}