* Konix NetList To Verilog Converter

This is a basic tool I knocked up to port the netlists over to verilog for the development of an FPGA Konix Multisystem Emulator. It's mostly to avoid the drudgery of doing it by hand, its setup for the 88 version of the netlists, I never got around to the 89 version, but in theory with a few adjustments it could be made to work.

It doesn't do any error checking, make sure the input folder is correct, and the output folder exists.

** Building

dotnet build

** Running

dotnet run <path to input folder (SS1NET)> <path to output folder>