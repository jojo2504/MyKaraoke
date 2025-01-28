import syncedlyrics

def search_lyrics():
    result = syncedlyrics.search(f"the line Twenty One Pilots", synced_only=True, providers=["NetEase"])
    return result  # Return the result

print(search_lyrics())