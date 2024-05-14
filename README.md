# Access Control demo

This C# demo using two u-blox ANT-B11 and C209 simulate an access control. 
The area will grant "access" the two azimuth angles are within a specic range.

Works with serial port or UDP broadcast packets.

![image](https://github.com/u-blox/access_control/assets/11769925/ceb33138-30ba-4585-9065-1f4e53805a4f)

Use the "Settings" checkbox to select the two comports.<br>
Hardware flow control is enabled by default

![image](https://github.com/u-blox/access_control/assets/11769925/25ada29b-e50e-4416-a771-33cde06b4479)

![image](https://github.com/u-blox/access_control/assets/11769925/451c6ada-de4d-406b-8eec-cd1135c8dfd4)

![demo](https://github.com/u-blox/access_control/assets/11769925/8c0ff16d-6ef7-471d-b5d0-7453efb2c994)


Select the Anchors Left and Right, is recommended to use Smooth of 3 packets, and using filter on MAC can also be needed if may Tags are available.<br>
![image](https://github.com/u-blox/access_control/assets/11769925/ee597158-36a0-4756-a2a3-c0ec2812fd7c)

## FTDI USB cable (special variant providing 3.3v to ANT-B11)
TTL-232RG-VREG3V3-WE: https://www.digikey.se/sv/products/detail/ftdi-future-technology-devices-international-ltd/TTL-232RG-VREG3V3-WE/2441361<br>

## Connector between ANT-B11 and FTDI cable
DR127D254P20F: https://www.digikey.se/sv/products/detail/chip-quik-inc/DR127D254P20F/5978282
 
![image](https://github.com/u-blox/access_control/assets/11769925/0cfd9733-4a0a-43b4-81d0-e3eb2c5294b4)

Compile the solution using Visual Studio 2017 or newer.

## Disclaimer
Copyright &copy; u-blox 

u-blox reserves all rights in this deliverable (documentation, software, etc., hereafter “Deliverable”).

u-blox grants you the right to use, copy, modify and distribute the Deliverable provided hereunder for any purpose without fee.

THIS DELIVERABLE IS BEING PROVIDED "AS IS", WITHOUT ANY EXPRESS OR IMPLIED WARRANTY. IN PARTICULAR, NEITHER THE AUTHOR NOR U-BLOX MAKES ANY REPRESENTATION OR WARRANTY OF ANY KIND CONCERNING THE MERCHANTABILITY OF THIS DELIVERABLE OR ITS FITNESS FOR ANY PARTICULAR PURPOSE.

In case you provide us a feedback or make a contribution in the form of a further development of the Deliverable (“Contribution”), u-blox will have the same rights as granted to you, namely to use, copy, modify and distribute the Contribution provided to us for any purpose without fee.
