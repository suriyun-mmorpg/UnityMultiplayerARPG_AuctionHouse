# UnityMultiplayerARPG_AuctionHouse
Auction house system extension for MMORPG KIT

It will connect to [mmorpg-kit-auction-house-service](https://github.com/insthync/mmorpg-kit-auction-house-service). So you have to setup it, try googling how to install Node.js, then follow the [instruction](https://github.com/insthync/mmorpg-kit-auction-house-service/blob/main/README.md).

## Deps
* Require [unity-rest-client](https://github.com/insthync/unity-rest-client)
* Require Newtonsoft.JSON, you can download the one which made for Unity from this https://github.com/jilleJr/Newtonsoft.Json-for-Unity. Or add `"com.unity.nuget.newtonsoft-json": "2.0.0` to `manifest.json` to use `Newtonsoft.JSON` package which made by Unity.

## How to use

After you add this extension to your project, it will have settings that important for auction house service connection there are:

- `auctionHouseServiceUrl` - Where is the auction house service, if you runs auction house at machine which have public IP is `128.199.78.31` and running on port `9800` (you can set port in `.env` file -> `PORT`), set this to `http://128.199.78.31:9800`
- `auctionHouseServiceUrlForClient` - It is the same thing with `auctionHouseServiceUrl` but this one will be sent to clients, and clients will use this value to connect to auction house service.
- `auctionHouseSecretKey` - Secret key which will be validated at auction house service to allow map-server to use functions. You can set secret keys in `.env` file -> `SECRET_KEYS`, if in the `.env` -> `SECRET_KEYS` value is `["secret1", "secret2", "secret3"]`, you can set this value to `secret1` or `secret2` or `secret3`.
