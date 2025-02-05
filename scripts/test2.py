from pynput import mouse
import pyautogui

def on_click(x, y, button, pressed):
    if button == mouse.Button.left and pressed:
        print ("Left clicked at position: ({}, {})".format(x, y))

# Set up the mouse listener
with mouse.Listener(on_click=on_click) as listener:
    listener.join()