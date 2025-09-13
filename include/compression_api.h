#pragma once

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
#define COMPRESSION_API __declspec(dllexport)
#else
#define COMPRESSION_API __attribute__((visibility("default")))
#endif

#include <stdint.h>

typedef struct {
    uint64_t original_size_bytes;
    uint64_t compressed_size_bytes;
    double compression_ratio;
    double compression_time_ms;
    double decompression_time_ms;
    double compression_speed_mbps;
    double decompression_speed_mbps;
    int success;
    char error_message[256];
} CompressionMetrics;

typedef enum {
    ALGORITHM_RLE = 0,
    ALGORITHM_HUFFMAN = 1,
    ALGORITHM_LZW = 2
} CompressionAlgorithm;

COMPRESSION_API int compress_file(
    CompressionAlgorithm algorithm,
    const char* input_file,
    const char* output_file,
    CompressionMetrics* metrics
);

COMPRESSION_API int decompress_file(
    CompressionAlgorithm algorithm,
    const char* input_file,
    const char* output_file,
    CompressionMetrics* metrics
);

COMPRESSION_API int get_file_size(const char* filename, uint64_t* size);

COMPRESSION_API const char* get_algorithm_name(CompressionAlgorithm algorithm);

COMPRESSION_API const char* get_last_error();

#ifdef __cplusplus
}
#endif