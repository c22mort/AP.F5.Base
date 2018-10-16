#==================================================================================
# Script: 	Get-DeviceMemoryUsedMb.ps1
# Date:		11/10/18
# Author: 	Andi Patrick
# Purpose:	Gets Device used Memory (Mb) via SNMP returns as Property Bag
#==================================================================================

# Get the named parameters
param($Debug,$DeviceAddress,$DevicePort,$DeviceCommunity,$SharpSnmpLocation)

#Constants used for event logging
$SCRIPT_NAME			= 'Get-DeviceMemoryUsedMb.ps1'
$EVENT_LEVEL_ERROR 		= 1
$VENT_LEVEL_WARNING 	= 2
$EVENT_LEVEL_INFO 		= 4

$SCRIPT_STARTED			= 4601
$PROPERTYBAG_CREATED	= 4602
$SCRIPT_EVENT			= 4603
$SCRIPT_ENDED			= 4604

#==================================================================================
# Sub:		LogDebugEvent
# Purpose:	Logs an informational event to the Operations Manager event log
#			only if Debug argument is true
#==================================================================================
function Log-DebugEvent
{
	param($eventNo,$message)

	$message = "`n" + $message
	if ($Debug -eq $true)
	{
		$api.LogScriptEvent($SCRIPT_NAME,$eventNo,$EVENT_LEVEL_INFO,$message)
	}
}

#Start by setting up API object.
$api = New-Object -comObject 'MOM.ScriptAPI'

# Log Startup Message
$message =	$SCRIPT_NAME + " Started."
Log-DebugEvent $SCRIPT_STARTED " Script Started."

# Load SharpSNMPLib
[reflection.assembly]::LoadFrom( (Resolve-Path $SharpSnmpLocation) )

# Use SNMP v2
$ver = [Lextm.SharpSnmpLib.VersionCode]::V2

# Set Walk Mode to WithinSubtree
$walkMode = [Lextm.SharpSnmpLib.Messaging.WalkMode]::WithinSubtree

# OIDs used
# bigipTrafficMgmt.bigipSystem.sysHostInfoStat.sysHostMemory.sysHostMemoryUsedKb
[string]$sysHostMemoryUsedKb = ".1.3.6.1.4.1.3375.2.1.7.1.4.0"

Try 
{

	# Create endpoint for SNMP server
	$ip = [System.Net.IPAddress]::Parse($DeviceAddress)
	$svr = New-Object System.Net.IpEndPoint ($ip, $DevicePort)

	# Create List for SNMP Get
	$vList = New-Object 'System.Collections.Generic.List[Lextm.SharpSnmpLib.Variable]'
	$oidMemoryUsed = New-Object Lextm.SharpSnmpLib.ObjectIdentifier ($sysHostMemoryUsedKb)
	$vList.Add($oidMemoryUsed)

	Try {
		# Get SNMP Results
		$results = [Lextm.SharpSnmpLib.Messaging.Messenger]::Get($ver, $svr, $DeviceCommunity, $vList, 3000)
		# As Long as we have one result we can continue
		if ($Results.Count -gt 0) {
			[int]$MemoryUsed = $results[0].Data.ToString()
			$MemoryUsed = $MemoryUsed / 1024
			#Create a property bag.
			$bag = $api.CreatePropertyBag()
			$bag.AddValue("MemoryUsed", [int]$MemoryUsed)
			[string] $message = " Created Property bag for $DeviceAddress`r`n`r`n" 	+ "MemoryUsed: "  + $MemoryUsed + " Mb`r`n"
			Log-DebugEvent $SCRIPT_EVENT $message
			#$api.Return($bag)
			$bag		
		}
	} Catch {
		Log-DebugEvent $SCRIPT_EVENT "SNMP Get Error : $_"
	}

}
Catch
{
	Log-DebugEvent $SCRIPT_EVENT "Could not Contact $DeviceAddress"
}

# Log Finished Message
Log-DebugEvent $SCRIPT_ENDED " Script Ended."