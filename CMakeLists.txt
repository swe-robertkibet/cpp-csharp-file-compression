cmake_minimum_required(VERSION 3.12)
project(MultiAlgorithmCompression LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Include directories
include_directories(include)
include_directories(external)

# Library source files (without main.cpp)
set(LIB_SOURCES
    src/rle.cpp
    src/huffman.cpp
    src/lzw.cpp
)

# Executable source files
set(EXE_SOURCES
    src/main.cpp
)

# Create shared library for C# interop
add_library(compression_lib SHARED ${LIB_SOURCES})

# Create executable
add_executable(compress ${EXE_SOURCES} ${LIB_SOURCES})

# Enable compiler warnings
if(MSVC)
    target_compile_options(compress PRIVATE /W4)
    target_compile_options(compression_lib PRIVATE /W4)
else()
    target_compile_options(compress PRIVATE -Wall -Wextra -Wpedantic)
    target_compile_options(compression_lib PRIVATE -Wall -Wextra -Wpedantic)
endif()

# Optional: Create tests (if you add test files later)
option(BUILD_TESTS "Build test programs" OFF)

if(BUILD_TESTS)
    enable_testing()
    
    set(TEST_SOURCES
        tests/test_rle.cpp
        src/rle.cpp
        src/huffman.cpp
        src/lzw.cpp
    )
    
    add_executable(test_rle ${TEST_SOURCES})
    target_include_directories(test_rle PRIVATE include external)
    
    add_test(NAME RLETests COMMAND test_rle)
endif()