# Hasher

A simple CLI file hasher utility implemented in C#.

Features a nice progress bar, and an asynchronous file hashing class, supporting all of the hashing algorithms the .Net framework supports.

This utility can use and detect the followings: MD5, SHA(1|256|384|512)

The input can be either a file and optionally a hash algorithm, or a file and a hexadecimal hash to test against.
Piping a file directly from the standard input is also supported.
