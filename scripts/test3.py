import requests
import os
from string import punctuation

website1 = "https://www.megalobiz.com"

def getDataFromURL(url):
    headers = {'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36'}
    webpage = requests.get(url, headers=headers)
    return webpage.text

def extractSongQueryFromFileName(mp3_file_name):
    mp3_file = mp3_file_name.replace(".mp3", "")
    for ch in mp3_file:
        if ch in punctuation or ch.isnumeric():
            mp3_file = mp3_file.replace(ch, " ")
    mp3_file = "+".join(mp3_file.split())
    return mp3_file

def scrap_search_result_links(search_page_data):
    linksarr = [] 
    link_start_index = 0
    link_end_index = 0
    iteration = 0
    while iteration <= 5:  
        link_start_index = search_page_data.find("href=\"/lrc", link_end_index) + 6
        link_end_index = search_page_data.find("\"", link_start_index)
        linksarr.append(search_page_data[link_start_index : link_end_index])
        iteration += 1
    linksarr.pop(0)  # Remove first link
    return linksarr

def scrap_lyrics(lyrics_page_data):
    lyrics_start_index = lyrics_page_data.find("[ve:v1.2.3]<br>") + 15
    lyrics_end_index = lyrics_page_data.find("</span>", lyrics_start_index)
    lyrics = lyrics_page_data[lyrics_start_index:lyrics_end_index].replace("<br>","")
    return lyrics

def create_lrc(mp3_file_name):
    song_query = extractSongQueryFromFileName(mp3_file_name)
    search_link = f"{website1}/search/all?qry={song_query}"
    
    try:
        search_page_data = getDataFromURL(search_link)
        search_result_links = scrap_search_result_links(search_page_data)
        
        # Use the first link
        lyrics_link = website1 + search_result_links[0]
        lyrics_page_data = getDataFromURL(lyrics_link)
        lyrics = scrap_lyrics(lyrics_page_data)

        lrc_file_name = mp3_file_name.replace(".mp3", ".lrc")
        with open(lrc_file_name, "w", encoding='utf-8') as lrc_file:
            lrc_file.write(lyrics)
        
        print(f"LRC file created for {mp3_file_name}")
    
    except Exception as e:
        print(f"Error creating LRC for {mp3_file_name}: {e}")

# Find and process MP3 files
mp3_files_list = [file for file in os.listdir(".") if file.endswith(".mp3")]
print(f"{len(mp3_files_list)} MP3 files found in current directory!")

for mp3_file in mp3_files_list:
    create_lrc(mp3_file)

print("Finished!!!")