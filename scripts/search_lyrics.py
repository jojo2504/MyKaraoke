import syncedlyrics

def search_lyrics_synced_only(arguments):
    track_name = arguments[0]
    artist_name = arguments[1]
    result = syncedlyrics.search(f"{track_name} {artist_name}", synced_only=True, providers=["NetEase"])
    return result  # Return the result

if __name__ == "__main__":
    # Assuming the arguments are passed in via command-line
    import sys
    sys.stdout.reconfigure(encoding='utf-8')
    if len(sys.argv) > 2:
        track_name = sys.argv[1]
        artist_name = sys.argv[2]
        result = search_lyrics_synced_only([track_name, artist_name])
        print(result)  # This will output the result for capturing in C#
    else:
        print("Invalid arguments!")