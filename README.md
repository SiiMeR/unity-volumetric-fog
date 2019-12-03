![Fog result 2019 dec.](docs/images/fogresult4.png?raw=true "")  

# Volumetric Fog
Volumetric fog implementation in Unity. Made originally as a part of my Bachelors thesis. Now I am just improving it in my free time. Does not currently support the new HD render pipeline that came in Unity 2018.

# How to run:
1. Clone this repository (`git clone https://github.com/SiiMeR/unity-volumetric-fog.git`)  
2. Download Unity (version 2018.3 and later supported. To use on earlier Unity versions, please clone an older commit, for example `3314461facc824a171fd859fdd95bab180fd8c48` supports 2018.1)
3. Open the project in Unity
4. Open the scene `Forest` (under the `Scenes` folder) and press play
5. Fog settings can be changed by selecting the `CameraAndFog` gameobject, which is a child of the `Player` gameobject in the scene hierarchy.

# Enabling benchmark mode
To enable benchmark mode, enable the Animator and Benchmark components under the 'Player' gameobject and disable the Player script. Additionally, enable the 'Text' gameobject under the 'Canvas' gameobject. Benchmark results are saved to AppData/LocalLow/DefaultCompany/unity-volumetric-fog on Windows.

# Default controls
W,S,A,D - Fly around  
Mouse look - Look around  
Shift - Fly faster  

# More screenshots
![Fog result image 1](docs/images/fogresult1.PNG?raw=true "")
![Fog result 2019 jan.](docs/images/Fogresult2019.png?raw=true "")  
![Fog result image 2](docs/images/fogresult2.PNG?raw=true "")
![Fog result image 3](docs/images/fogresult3.PNG?raw=true "")
