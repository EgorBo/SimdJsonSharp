OUTPUT_DIR=../../../Bin/

.PHONY: libSimdJsonNative.dylib

libSimdJsonNative.dylib:
	mkdir -p $(OUTPUT_DIR) && clang simdjson.cpp bindings.cpp -O3 -march=haswell -std=c++17 -dynamiclib -undefined dynamic_lookup -o $(OUTPUT_DIR)libSimdJsonNative.dylib
