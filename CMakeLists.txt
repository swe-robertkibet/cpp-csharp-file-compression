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
    src/compression_api.cpp
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

# Installation configuration
install(TARGETS compress DESTINATION bin)
install(TARGETS compression_lib DESTINATION lib)
install(DIRECTORY include/ DESTINATION include)

# Package configuration for Windows installer
set(CPACK_PACKAGE_NAME "MultiAlgorithmCompression")
set(CPACK_PACKAGE_VENDOR "Multi-Algorithm Compression Tool")
set(CPACK_PACKAGE_DESCRIPTION_SUMMARY "A C++ implementation of RLE, Huffman, and LZW compression algorithms")
set(CPACK_PACKAGE_VERSION "1.0.0")
set(CPACK_PACKAGE_VERSION_MAJOR "1")
set(CPACK_PACKAGE_VERSION_MINOR "0")
set(CPACK_PACKAGE_VERSION_PATCH "0")
set(CPACK_PACKAGE_INSTALL_DIRECTORY "MultiAlgorithmCompression")

# Windows-specific packaging
if(WIN32)
    set(CPACK_GENERATOR "NSIS;ZIP")
    set(CPACK_NSIS_DISPLAY_NAME "Multi-Algorithm Compression Tool")
    set(CPACK_NSIS_PACKAGE_NAME "Multi-Algorithm Compression Tool")
    set(CPACK_NSIS_CONTACT "developer@example.com")
    set(CPACK_NSIS_MODIFY_PATH ON)
    set(CPACK_NSIS_ENABLE_UNINSTALL_BEFORE_INSTALL ON)

    # Add desktop shortcut
    set(CPACK_NSIS_MENU_LINKS
        "bin/compress.exe" "Multi-Algorithm Compression Tool (Console)"
    )

    # Set the icon for the installer
    # set(CPACK_NSIS_MUI_ICON "${CMAKE_CURRENT_SOURCE_DIR}/assets/icon.ico")
    # set(CPACK_NSIS_MUI_UNIICON "${CMAKE_CURRENT_SOURCE_DIR}/assets/icon.ico")
else()
    set(CPACK_GENERATOR "TGZ")
endif()

# Include system libraries
include(InstallRequiredSystemLibraries)

# Include CPack
include(CPack)