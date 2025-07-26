# SWaffleCon - PEAK Pluguin

SWaffle Console is an external debugging tool for them game PEAK. This plugin is a custom made plugin for myself (Shasta) that I am wanting to share with others who may find it interesting or may want to utilize it themselves. This is an ongoing project and I have many plans to adapt it.

Caution: Plugin may cause crashing - Start game first and then open client.

### Thunderstore Packaging

This template comes with Thunderstore packaging built-in, using [TCLI](<https://github.com/thunderstore-io/thunderstore-cli>).

You can build Thunderstore packages by running:

```sh
dotnet build -c Release -target:PackTS -v d
```

> [!NOTE]  
> You can learn about different build options with `dotnet build --help`.  
> `-c` is short for `--configuration` and `-v d` is `--verbosity detailed`.

The built package will be found at `artifacts/thunderstore/`.
