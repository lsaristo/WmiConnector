INTRODUCTION:  
  Since there didn't seem to be any decent package built in for system
  administrators to run home-made backup utilities against all systems in 
  a domain and to track those backups, I created a simple program to do just
  that. 

REMARKS:
  There are two primary components to this program:
    1. A master coordinator that connects to remote machines and handles 
       reporting and file management. 
    2. A simple C program that reports back to the coordinator from a host
       machine using Winsock.
