Home.Pi
=======
This is a pet project to control some devices inside my home. This is mostly a project to stretch my legs with .NET and get to know some of the newer features of the framework. This project likely contains pieces that are over engineered as well as some pieces that are under engineered. 

The basic principal of the stack is made of two components that talk to each other via an Azure Storage Queue:  
- **Home.Pi.Server**  
An Azure Functions API to handle requests from services like IFTTT or from Google Home.  
- **Home.Pi.Daemon**  
A systemd service that runs on a Rasberry Pi running on my local network.

