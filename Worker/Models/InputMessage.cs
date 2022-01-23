namespace Worker.Models;

public record InputMessage(
    string InputFileName,
    string DesiredExtension
);