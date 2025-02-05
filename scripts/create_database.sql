CREATE TABLE IF NOT EXISTS Songs (
    SongId INTEGER PRIMARY KEY AUTOINCREMENT,
    Title TEXT NOT NULL,
    Artist TEXT NOT NULL,
    VocalHash TEXT NOT NULL,
    MusicHash TEXT NOT NULL,
    LRCHash TEXT,
    FOREIGN KEY(VocalHash) REFERENCES Files(FileHash),
    FOREIGN KEY(MusicHash) REFERENCES Files(FileHash)
    FOREIGN KEY(LRCHash) REFERENCES Files(FileHash)
);  

CREATE TABLE IF NOT EXISTS Files (
    FileHash TEXT PRIMARY KEY
);

CREATE TABLE IF NOT EXISTS Playlists (
    PlaylistID INTEGER PRIMARY KEY AUTOINCREMENT,
    PlaylistName TEXT NOT NULL
)

CREATE TABLE IF NOT EXISTS PlaylistSongs (
    PlaylistID INTEGER,
    SongId INTEGER,
    PRIMARY KEY (PlaylistID, SongId),
    FOREIGN KEY (PlaylistID) REFERENCES Playlists(PlaylistID),
    FOREIGN KEY (SongId) REFERENCES Songs(SongId)
);

CREATE TABLE IF NOT EXISTS PlaylistHierarchy (
    ParentPlaylistID INTEGER,
    ChildPlaylistID INTEGER,
    PRIMARY KEY (ParentPlaylistID, ChildPlaylistID),
    FOREIGN KEY (ParentPlaylistID) REFERENCES Playlists(PlaylistID),
    FOREIGN KEY (ChildPlaylistID) REFERENCES Playlists(PlaylistID)
);