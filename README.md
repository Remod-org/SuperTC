# SuperTC
SuperTC Looks tough and protects tough

When deployed, SuperTC protects itself with a toptier and stone shell as well as code that actually does the protection.

Not only that, but buildings and deployables in the standard protection range are also protected absolutely from any non-decay damage.

This may be best utilized as a boost on a PvP server, or for a PvE-by-agreement server where full protection is needed to protect specific buildings.

Usage on a PvE server, e.g. in conjunction with NextGenPVE, might be just a bit silly.  Oh well, it still looks cool!

If used with NoDecay, the building would also be impervious to decay.  Note that SuperTC does NOT protect the building from decay.

![](https://imgur.com/ethO5AT.jpg)

## Commands

If requirePermission is true in the config, a player with permission has access to the following command:

 - /stc -- Enable or disable deployment of a SuperTC when deploying TC.  Running the command shows the resulting status.

## Permissions

 - supertc.use -- If requirePermission is true, this permission will be required for deployment of SuperTCs

## Configuration

```json
{
  "allTCs": false,
  "adminTCs": false,
  "defaultEnabled": true,
  "requirePermission": false,
  "debug": true,
  "Version": {
    "Major": 1,
    "Minor": 0,
    "Patch": 1
  }
}
```

  - allTCs -- Spawn a SuperTC for any and all spawned tool cupboards
  - adminTCs -- Spawn a SuperTC for admins
  - defaultEnabled -- If requirePermission is set, default to enabled.  The player can disable this with the /stc command
  - requirePermission -- require permission to allow deploying a SuperTC

