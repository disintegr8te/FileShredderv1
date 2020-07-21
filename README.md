# FileShredderv1 - Advanced Silent Encryption of Files on Windows

Performs Encryption and Decryption of Files with Pub/Private Key on Windows and placed an randomized Ransomnote.
The Program tries to mimic an more advanced Ransomware-Attack.
Goal is to develop better Detection mechanism for Detecting the File Encyption Process.


Most Ransomware is easly to Detect and most Ransomware Detection Methods are relying on the following:
* File - Extension
* File Header (First Bytes)
* Ransomware Notes
* Encryption Speed


This Project tries to level this up.
* Randomizes Ransom Note Creation
* Slows down the Encryption Process
* Uses Valid File Headers
* Uses Valid File Extensions
* Uses Private/Public Key Crypto


## Usage
Usage to Encrypt Files:
```bash
./FileShredderv1.exe
```
Supply Path to the Folder to Encrypt in the Console and Confirm.

Usage to decrypt Files:
```bash
./FileShredderv1-Decrypter.exe
```
Supply Path with the Encrypted Files in the Console and Confirm.


To Supply an own Pub/PrivKey change the "private const string publicKey" and "private const string privateKey" in the Source.



## Required:
.Net Runtime 4.0

## Developer:
disintegr8te

## Contributing
Pull requests are welcome. Please open an issue first to discuss what you would like to change.
Please make sure to update tests as appropriate.
