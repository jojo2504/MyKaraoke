import syncedlyrics

def search_lyrics_synced_only(arguments):
    track_name = arguments[0]
    artist_name = arguments[1]
    result = syncedlyrics.search(f"{track_name} {artist_name}", synced_only=True, providers=["NetEase"])
    return result

print(search_lyrics_synced_only(["Spin the Wheel", "Mick Wingert"]))
