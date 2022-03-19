namespace Worker.Models;

public record InputMessage(
    int Id,
    string InputFilePath,
    string Arguments,
    string OutputFilePath
);