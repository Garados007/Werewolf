FROM bitnami/git:latest as version
WORKDIR /src
COPY ./.git ./.git
COPY ./version.txt ./
RUN echo "$(cat version.txt)-$(git rev-parse --short HEAD)" > version

FROM ubuntu:latest as builder
RUN apt-get -qq update -y && \
    apt-get -qq upgrade -y && \
    apt-get install wget -y && \
    wget -q https://github.com/elm/compiler/releases/download/0.19.1/binary-for-linux-64-bit.gz \
        -O elm.gz && \
    gunzip elm.gz && \
    chmod +x elm && \
    mv elm /bin/
WORKDIR /src
COPY ./elm.json /src/
COPY ./ui /src/ui
COPY ./preprocess-elm.sh /src/
COPY --from=version /src/version ./version
RUN chmod +x preprocess-elm.sh && \
    ./preprocess-elm.sh && \
    cd bin && \
    rm -r ui/common/Debug && \
    echo "s/version = \\\".*\\\"/version = \\\"$(cat "/src/version")\\\"/" && \
    sed -i "s/version = \\\".*\\\"/version = \\\"$(cat "/src/version")\\\"/" \
        ui/common/Config.elm && \
    cat ui/common/Config.elm && \
    mkdir /content && \
    mkdir /content/special && \
    elm make --optimize --output=/content/index.js ui/game/Main.elm && \
    elm make --optimize --output=/content/special/translation.html ui/translation/Translation/Main.elm

FROM node:latest as js-compressor
RUN npm install uglify-js --global
WORKDIR /content
COPY --from=builder /content/index.js /content/source.js
RUN uglifyjs \
        source.js \
        --compress 'pure_funcs=[F2,F3,F4,F5,F6,F7,F8,F9,A2,A3,A4,A5,A6,A7,A8,A9],pure_getters,unsafe_comps,unsafe' \
        --mangle 'reserved=[F2,F3,F4,F5,F6,F7,F8,F9,A2,A3,A4,A5,A6,A7,A8,A9]' \
        --output index.js && \
    rm source.js

FROM node:latest as css-compressor
RUN npm install -g csso-cli && \
    npm install -g clean-css-cli
WORKDIR /content
COPY ./content /content
RUN mkdir -p bin && \
    cd css && \
    cp style.css orig.style.css && \
    cp orig.style.css ../bin/ && \
    cleancss --inline all -O2 --source-map \
        -o style.min.css \
        orig.style.css && \
    cd .. && \
    csso -i css/style.min.css \
        --input-source-map css/style.min.css.map \
        -o bin/style.css \
        -s file \
        -u css/usage.json

FROM bitnami/git:latest as vendor
WORKDIR /src
RUN apt-get update && \
    apt-get install -y jq
COPY . .
RUN git submodule update --init --recursive

FROM bitnami/java:latest as grammar-builder
WORKDIR /src
COPY ./tools/LogicCompiler/grammar ./
COPY ./tools/LogicCompiler/antlr-4.13.1-complete.jar ./
RUN java -jar antlr-4.13.1-complete.jar -message-format vs2005 -long-messages -Werror -Dlanguage=CSharp -no-listener -package LogicCompiler.Grammar W5LogicLexer.g4 W5LogicParser.g4

FROM mcr.microsoft.com/dotnet/sdk:8.0 as logic-builder
WORKDIR /src
COPY ./Werewolf.sln ./Werewolf.sln
# build tools
COPY ./tools ./tools
COPY --from=grammar-builder /src ./tools/LogicCompiler/grammar
RUN mkdir -p /tools && \
    dotnet publish --nologo -c RELEASE -o /tools \
        tools/LogicCompiler/LogicCompiler.csproj
# build logic files
COPY ./logic ./logic
RUN find /src/logic/ -mindepth 1 -maxdepth 1 -type d | \
        xargs -I {} basename {} | \
        while read -r name; do \
            dotnet /tools/LogicCompiler.dll \
                --no-build \
                --write-info-path /data/themes/${name}.json \
                --source /src/logic/${name} \
                --target /src/server/Theme/${name} \
                --namespace Theme.${name} || exit $?; \
            echo -n " --mode /data/themes/${name}.json" >> /data/themes/call; \
        done

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS lang-builder
COPY --from=vendor /src/content/vendor/flag-icon-css /src/content/vendor/flag-icon-css
COPY ./tools/LogicTools /src/tools/LogicTools
COPY ./tools/LangConv /src/tools/LangConv
WORKDIR /src/tools/LangConv
RUN dotnet build
COPY --from=logic-builder /data/themes /data/themes
COPY ./content/lang/raw /src/content/lang/raw
RUN dotnet run -d /src/content/lang --no-print-missing-lang-string-warning $(cat /data/themes/call) && \
    rm -r /src/content/lang/raw

# FROM mcr.microsoft.com/dotnet/sdk:6.0 as report
# # RUN apt-get update && \
# #     apt-get install -y python3-pip nodejs npm && \
# #     ln /usr/bin/pip3 /usr/bin/pip && \
# #     pip install translators --upgrade
# WORKDIR /src
# COPY ./Translate ./Translate
# # RUN cd /src/Translate/Bing && \
# #     npm install
# COPY ./Themes ./Themes
# COPY ./Werewolf ./Werewolf
# COPY ./Test ./Test
# COPY ./Werewolf.sln ./Werewolf.sln
# COPY ./content ./content
# RUN cd /src/Translate && \
#     dotnet run --report-only

FROM httpd:2.4
COPY --from=js-compressor /content /usr/local/apache2/htdocs/content
COPY --from=builder /content/special /usr/local/apache2/htdocs/content/special
COPY --from=vendor /src/content /usr/local/apache2/htdocs/content
COPY --from=css-compressor /content/bin /usr/local/apache2/htdocs/content/css
COPY ./test-report.html /usr/local/apache2/htdocs/content/test-report.html
COPY ./docker/httpd.conf /usr/local/apache2/conf/httpd.conf
COPY --from=version /src/version /usr/local/apache2/htdocs/content/version
RUN rm -r /usr/local/apache2/htdocs/content/lang
COPY --from=lang-builder /src/content/lang /usr/local/apache2/htdocs/content/lang
# COPY --from=report /src/content/report /usr/local/apache2/htdocs/content/report
RUN ver="\$(cat /usr/local/apache2/htdocs/content/version)" && \
    sed -i "s@/content/index.js@/content/index.js?_v=\$ver@" \
        /usr/local/apache2/htdocs/content/index.html
