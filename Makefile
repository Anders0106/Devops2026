.PHONY: build clean restore watch run test
build:
	dotnet build
clean:
	dotnet clean
restore:
	dotnet restore
watch:
	dotnet watch --project ./src/Chirp.CLI/ $(type) $(args)
run:
	dotnet run --project ./src/Chirp.CLI/ $(type) $(args)
test:
	dotnet test