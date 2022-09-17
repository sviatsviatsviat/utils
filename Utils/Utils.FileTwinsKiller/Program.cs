using System.Text.RegularExpressions;

/* 
 * Util takes two parameters: directory and pattern of file.
 * It scans matching files in the directory and deletes extra identical copies.
 */

const int COMPARISON_BUFER_SIZE = 64;

// TODO: by default can be used Working Directory.
string dir = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : throw new ArgumentNullException("Directory should be set");
string imageFilePattern = args.Length > 1 ? args[1] : @".*";

Regex validator = new Regex(imageFilePattern);
var images = Directory.GetFiles(dir, "*", SearchOption.AllDirectories).Where(file => validator.IsMatch(file)).ToArray();
HashSet<string> duplicates = new HashSet<string>();

Console.WriteLine("Found {0} images", images.Length);

for (int i = 0; i < images.Length; i++)
{
    var currentCursorPossition = Console.GetCursorPosition();

    Console.SetCursorPosition(0, 0);
    Console.WriteLine("Scanning {0} out of {1}", i + 1, images.Length);
    Console.SetCursorPosition(currentCursorPossition.Left, currentCursorPossition.Top);

    // NOTE: it is better to load current file only once.
    // TODO: an optimization can be implemented here to not load file fully if it is too large.
    var imageBytes = await File.ReadAllBytesAsync(images[i]);

    Parallel.For(i + 1, images.Length, async j =>
    {
        var filesAreEqual = await CompareBytesToFile(imageBytes, images[j], COMPARISON_BUFER_SIZE);
        if (filesAreEqual)
        {
            duplicates.Add(images[j]);
        }
    });
}

Console.WriteLine("Found {0} duplicates", duplicates.Count);
// TODO: list of duplicates can be printed here.

var lastCursorPosition = Console.GetCursorPosition();

foreach (var duplicate in duplicates)
{
    // TODO: Add some interaction here. E.g. chose (delete, delete all, skip, skip all).
    Console.SetCursorPosition(lastCursorPosition.Left, lastCursorPosition.Top);
    Console.WriteLine("Deleting {0}", Path.GetFileName(duplicate));
    File.Delete(duplicate);
}

Console.WriteLine("Finished");
Console.ReadLine();

async Task<bool> CompareBytesToFile(byte[] bytes, string filePath, int bufferSize)
{
    using var file = File.OpenRead(filePath);

    if (bytes.Length != file.Length)
        return false;

    int startPosition = 0;
    byte[] buffer = new byte[bufferSize];
    while (file.Position < file.Length)
    {
        var bytesRead = await file.ReadAsync(buffer);
        int endPosition = startPosition + bytesRead;
        if (!Enumerable.SequenceEqual(bytes[startPosition..(endPosition)], buffer[0..bytesRead]))
        {
            return false;
        }
        startPosition = endPosition;
    }

    return true;
}

