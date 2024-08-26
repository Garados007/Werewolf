import translators as ts
import warnings
import sys
warnings.filterwarnings("ignore")

print(
    ts.google(
        sys.argv[1],
        from_language=sys.argv[2],
        to_language=sys.argv[3]
    ),
    end=""
)
