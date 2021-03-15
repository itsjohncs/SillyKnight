.PHONY: .FORCE clean

bin/Debug/SillyKnight.dll: .FORCE
	msbuild /nowarn:MSB3277 SillyKnight.csproj

clean:
	rm -r bin/ obj/
