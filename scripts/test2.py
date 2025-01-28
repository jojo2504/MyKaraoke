import requests
import os
from string import punctuation
import time

search_link_iteration_count = 5

website1 = "https://www.megalobiz.com"

clear_screen_command = "cls" if  os.name == "nt" else "clear"

def clear_screen():
    os.system(clear_screen_command)

def getDataFromURL(url):
    # Site 2 rejects GET requests from python that do not identify as user agent
    # Thus, custom user-agent header needs to be defined to open the link properly 
    headers = {'User-Agent': 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_11_5) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36'}
    webpage = requests.get(url, headers=headers)
    return webpage.text

def scrap_search_result_links_from_site_1(first_search_page_data):
    linksarr = [] 
    link_start_index = 0
    link_end_index = 0
    iteration = 0
    while iteration <= search_link_iteration_count:  
                        link_start_index = first_search_page_data.find("href=\"/lrc", link_end_index) + 6
                        link_end_index = first_search_page_data.find("\"", link_start_index)
                        linksarr.append(first_search_page_data[link_start_index : link_end_index])
                        iteration = iteration + 1
    linksarr.pop(0)         #first link is not useful
    return linksarr

def scrap_lyrics_from_site_1(first_lyrics_page_data):
    lyrics_start_index = first_lyrics_page_data.find("[ve:v1.2.3]<br>") + 15
    lyrics_end_index = first_lyrics_page_data.find("</span>", lyrics_start_index)
    
    lyrics = first_lyrics_page_data[lyrics_start_index:lyrics_end_index].replace("<br>","")
    return lyrics

def extractSearchResultLinks(mp3_to_search):
    song_query = mp3_to_search
    search_link1 = "https://www.megalobiz.com/search/all?qry=" + song_query
    
    first_search_page_data = getDataFromURL(search_link1)

    linksarr = []
    linksarr.extend(scrap_search_result_links_from_site_1(first_search_page_data) )
    return linksarr
    
def getLyricsLink(mp3_file):
    search_result_links = extractSearchResultLinks(mp3_file)
    serialnumber = 1
    for link in search_result_links:
        print(str(serialnumber) + ". "+ link)
        serialnumber += 1
    return (website1 + search_result_links[0], "1")    #Return tuple containing link and another member to indicate website


def getlyrics(mp3_file):
    lyrics_link_tuple = getLyricsLink(mp3_file)
    if(lyrics_link_tuple[0] == ""):
        return ""
    lyrics_page_data = getDataFromURL(lyrics_link_tuple[0])
    lyrics = scrap_lyrics_from_site_1(lyrics_page_data)
    return lyrics

def get_lrc(mp3_file_name):
    lyrics = getlyrics(mp3_file_name)
    print(lyrics)

get_lrc("You Set My World On Fire - Loving Caliber")