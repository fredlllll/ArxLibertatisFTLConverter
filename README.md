# ArxLibertatisFTLConverter
converts FTL files into wavefront object files, and vice versa 


Compilation instructions:

There is a .sln file present, confirmed to work with VS2019 with one minor caveat. 
The nuget packages do not initialize automatically, if you do not have nuget configured. 

You will want to do the following (if it doesn't work automatically for you)
![image](https://user-images.githubusercontent.com/991507/189041624-bb8cf607-9db0-427e-ae3b-f05d80ae43ca.png)

Set the package source in the nuget configuration to nuget rather than "Microsoft visual studio offline packages"
![image](https://user-images.githubusercontent.com/991507/189041725-84d2b6aa-6eff-41a8-a5ac-9be939926764.png)

If Nuget is not visible in the drop down you will have to add it manually which is trivial to do. 
Simply click the cogwheel next to the drop down, This will open the options for the package sources. 
Click the plus button and add in the pertinent nuget package source information
![image](https://user-images.githubusercontent.com/991507/189041920-aa6c9054-f1d5-4115-b35d-17a15a3384d0.png)

https://api.nuget.org/v3/index.json

You should be able to build at this point. 


Usage Instructions:

Before use you will have to supply the arx IO dll found in the bin directory of the game. "ArxLibertatisEditorIO.dll"
Copy this to the build directory
![image](https://user-images.githubusercontent.com/991507/189042280-2dc4e3b0-1240-45aa-a743-f14137784fc4.png)


The converter is invoked from the command prompt, simply pass in a path for conversion.
 Note: For ftl to object conversion, the files must be presented in situ, in the game directory for the converter to find the root game directory
![image](https://user-images.githubusercontent.com/991507/189042454-d7c437e7-968c-4e0a-b3e6-b7c77692d31e.png)

Note: For obj to ftl conversion, the game directory does not appear to be required


