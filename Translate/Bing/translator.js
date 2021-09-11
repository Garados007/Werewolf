const { translate } = require('bing-translate-api');
const UserAgent = require('user-agents');

if (process.argv.length != 5) {
    console.error("3 required arguments missing");
    return;
}

var userAgent = new UserAgent().toString();
translate(
    process.argv[2], // text
    process.argv[3], // from
    process.argv[4], // to
    false,           // correct
    false,           // raw
    userAgent        // User-Agent
).then(res => {
    console.log(
        JSON.stringify({
            result: res.translation
        })
    );
}).catch(err => {
    console.log(
        JSON.stringify({
            err: err
        })
    );
});
