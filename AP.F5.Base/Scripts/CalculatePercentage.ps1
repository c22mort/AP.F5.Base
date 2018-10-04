#==================================================================================
# Script: 	CalculatePercentage.ps1
# Date:		14/05/18
# Author: 	Andi Patrick
# Purpose:	Calculates the percentage of a given number against another given number.
#==================================================================================

# Named Parameters
param($Number, $Total)

#Start by setting up API object.
$api = New-Object -comObject 'MOM.ScriptAPI'


#Create a property bag.
$bag = $api.CreatePropertyBag()

# Add Percentage Value to Property Bag
[int]$Percentage = ($Number / $Total) * 100
$bag.AddValue('Percentage', $Percentage)

# Return Property Bag
#$api.Return($bag)
$bag

