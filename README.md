# [Exploring Code Project](https://theolivenbaum.medium.com/exploring-codeproject-with-curiosity-4f197ceaa831) 🚀

This repository contains the source code presented in the [Exploring CodeProject with Curiosity](https://theolivenbaum.medium.com/exploring-codeproject-with-curiosity-4f197ceaa831) article. It uses [Curiosity's](https://curiosity.ai) AI-powered search and it's [data connector](https://www.nuget.org/packages/Curiosity.Library) library to explore and search on CodeProject's articles, using machine-learning-based synonyms, and to suggest similar articles using word and graph embeddings.

The data used for the demo was scraped from the [CodeProject](https://codeproject.com) website, and includes authors and their articles. 

Check more details on our accompanying [blog post](https://theolivenbaum.medium.com/exploring-codeproject-with-curiosity-4f197ceaa831).

## Running Curiosity Locally

[Check our documentation](https://docs.curiosity.ai/en/articles/4449019-installation) to install a free instance of Curiosity on your computer or clould environment of preference.

Once you have your Curiosity instance up and running, check the [initial setup guide](https://docs.curiosity.ai/en/articles/4452603-initial-setup) and then you'll be ready to use the code in this repository.

## Data Ingestion

The code in this repository will crawl and ingest all articles from CodeProject to your Curiosity instance.

You'll need to generate an API token for your system, and pass it to the connector. Check the [documentation on how to create an API token](https://docs.curiosity.ai/en/articles/4453131-external-data-connectors).

```bash
git clone https://github.com/curiosity-ai/codeproject-search
cd codeproject-search
dotnet run {SERVER_URL} {AUTH_TOKEN}
```

You need to replace `{SERVER_URL}` with the address your server is listing to (usually `http://localhost:8080` if you're running it locally), and `{AUTH_TOKEN}` with the API token generated earlier.
