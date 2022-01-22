namespace Worker.Models;

public record InputMessage(
    string InputFilePath,
    string OutputFilePath
);