# Sensorberg Showcase app for Windows 10 #

Sensorberg Showcase demonstrates the full capabilities of Sensorberg SDK. Rather
than being a code sample, it is designed for testing of both the SDK itself and
other applications. With Sensorberg Showcase you can verify that your API key is
valid and that you beacons and campaigns are defined correctly.

![Sensorberg Showcase running on Lumia phone](/Screenshots/SensorbergShowcaseScanner360.png)&nbsp;
![Sensorberg Showcase running on Lumia phone](/Screenshots/SensorbergShowcaseAdvertise360.png)&nbsp;
![Sensorberg Showcase running on Lumia phone](/Screenshots/SensorbergShowcaseSettings360.png)

![Sensorberg Showcase running on Surface 3](/Screenshots/SensorbergShowcaseLargeDisplayScaled.png)

This showcase application has the following features (amongst others):

* Beacon scanner with extensive user interface displaying the details of scanned beacons
* Advertiser, which allows you to set the desired beacon IDs making your device act as a beacon
* Dynamic API key - if you have more than one applications, you can switch the API key dynamically or reset back to the demo API key

## Compatibility ##

Sensorberg Showcase app can be run on all Windows 10 devices (phones, tablets,
laptops, desktops).

## Building the project ##

### Prerequisites ###

* Install the latest Visual Studio: https://www.visualstudio.com/
* Install the SQLite VSIX package for Universal Windows Platform development using Visual Studio 2015 (`sqlite-uwp-<version number>.vsix`): http://www.sqlite.org/download.html
* Install the Sensorberg SDK VSIX package: TBD

### Building and running the project ###

1. Open the project solution file, `SensorbergShowcase.sln`, located in the root
   directory of this project.
2. Make sure the **SQLite** and **Sensorberg SDK** references are added to the project
   (see the images below).

   ![Adding references in Solution Explorer](/Documentation/Images/VisualStudioAddingReference.png)&nbsp;
   ![Selecting extensions](/Documentation/Images/VisualStudioSelectExtensions.png)&nbsp;
   ![References added](/Documentation/Images/VisualStudioRefencesAdded.png)

3. Select the platform you are building the app for, e.g.:
 * **ARM** for Lumia phones
 * **x86** or **x64** for laptops and desktops

4. Build the solution (**Build** -> **Build Solution** or Ctrl+Shift+B)
