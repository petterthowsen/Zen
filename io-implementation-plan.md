# Zen IO Standard Library Implementation Plan

## Overview

This document outlines the implementation plan for Zen's IO standard library, focusing on file operations, binary data handling, and context management through the 'with' statement.

## 1. Core Interfaces

### IContext Interface (✓ Completed)
```zen
interface IContext {
    _EnterContext(): IContext
    _ExitContext(): void
}
```

## 2. File System Classes

### File Class (✓ Completed)
- Implemented async operations using System.IO.FileStream
- Added proper context management with IContext
- Implemented text operations (ReadText, WriteText, etc.)
- Implemented binary operations using int arrays (until byte type is available)
- Added proper resource cleanup in _ExitContext

### FileInfo Class (✓ Completed)
- Created wrapper around System.IO.FileInfo
- Implemented all metadata properties
- Added file manipulation methods
- Added utility methods for opening files

### Directory Class (✓ Completed)
- Created wrapper around System.IO.Directory
- Implemented static methods for directory operations
- Added proper error handling
- Added utility methods for path operations

## 3. Testing (✓ Completed)

Created comprehensive test suite in tests/Zen.Tests/Execution/IOTests.cs covering:
- File operations (read/write text and binary)
- File context management ('with' statement)
- FileInfo metadata and operations
- Directory operations
- Error conditions and resource cleanup

## 4. Example Usage

```zen
# Text File Operations
async func example1() {
    with file = await File.Open("example.txt", "w") {
        await file.WriteText("Hello, World!")
    }

    with file = await File.Open("example.txt") {
        var content = await file.ReadText()
        print content  # Hello, World!
    }
}

# Binary File Operations
async func example2() {
    with file = await File.Open("data.bin", "w") {
        await file.WriteBytes([65, 66, 67])  # ABC in ASCII
    }
}

# Directory Operations
async func example3() {
    if !Directory.Exists("temp") {
        await Directory.Create("temp")
    }
    
    var files = Directory.GetFiles("temp")
    for file in files {
        var info = FileInfo.GetInfo(file)
        print info.Name, info.Length
    }
}
```

## 5. Future Enhancements

1. **Byte Type Support**
   - Once byte type is implemented, update binary operations to use byte instead of int
   - Add byte array support for efficient binary data handling

2. **Stream Abstractions**
   - Implement MemoryStream for in-memory operations
   - Add NetworkStream for network IO
   - Create abstract Stream base class

3. **Advanced Features**
   - File watching/monitoring capabilities
   - Memory-mapped file support
   - Compression integration
   - Encryption support

## 6. Implementation Status

✓ Complete:
1. Core File class async operations
2. Binary data support (using int arrays)
3. FileInfo wrapper implementation
4. Directory operations
5. Test suite

□ Future Work:
1. Byte type support
2. Stream abstractions
3. Advanced features (file watching, compression, etc.)
4. Additional utility methods as needed
