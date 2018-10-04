#==================================================================================
# Script: 	Subtract Div1024.ps1
# Date:		14/05/18
# Author: 	Andi Patrick
# Purpose:	Subtracts Number from another Number and divides result by 1024.
#==================================================================================

# Named Parameters
param($Number1, $Number2)

#Start by setting up API object.
$api = New-Object -comObject 'MOM.ScriptAPI'

#Create a property bag.
$bag = $api.CreatePropertyBag()

# Create Result
$Result = $Number1 - $Number2
$Result = [int]($Result / 1024)

# Add Result to Property Bag
$bag.AddValue('Number', $Result)

# Return Property Bag
#$api.Return($bag) # Used for debugging
$bag