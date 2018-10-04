#==================================================================================
# Script: 	Div1024.ps1
# Date:		14/05/18
# Author: 	Andi Patrick
# Purpose:	Divides a given number by 1024.
#==================================================================================

# Named Parameters
param($Number)

#Start by setting up API object.
$api = New-Object -comObject 'MOM.ScriptAPI'


#Create a property bag.
$bag = $api.CreatePropertyBag()

# Add Percentage Value to Property Bag
$bag.AddValue('Number', [int]($Number / 1024))

# Return Property Bag
#$api.Return($bag)
$bag