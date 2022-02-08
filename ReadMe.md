Home.Pi
=======
This is a pet project to control some devices inside my home. This was created to solve a single issue initially (Be able to say "Hey Google, turn my computer on") and to stretch my legs with .NET and get to know some of the newer features of the framework. This project likely contains the following:
- Typos
- Profanity laden comments
- Underengineered hacks
- Overengineered hacks
- Gramatical errors

The basic architecture is made up of two components that talk to each other via an Azure Storage Queue:  
- **Home.Pi.Server**  
An Azure Functions API to handle requests from services like IFTTT or from Google Home.  
- **Home.Pi.Daemon**  
A systemd service that runs on a Rasberry Pi running on my local network.

## Current Functionality
- Issue a WOL Packet to a PC
- Issue control messages to [shelf.pi](https://github.com/jesseryoung/shelf.pi)