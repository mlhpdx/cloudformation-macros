# Cloudformation Macros

This repo contains macros that help me create more concise templates.  AWS provides [some macros and tranformations](https://github.com/aws-cloudformation/aws-cloudformation-macros) as well, but they don't cover some obvious use cases.  See below for details on the macros herewithin.

## Deployment

This is an AWS SAM project, so it's the usual `sam build && sam deploy`.  There is a [deploy script](scripts/deploy-global.sh) in the scripts folder but since this project is setup for CodeBuild and using ParameterStore you might need to configure some parameters to match your own bucket name/prefix.

## ForEach

This macro is similar to the `Count` macro from AWS, but instead of simply using an incrementing number it allows you to specific a comma deliminted list parameter and instantiates the annotated object once for each item in the list.  It does the same replacement of `%d` with the instance index number, but replaces `%v` with the value from the list.  This allows some pretty convenient patterns, a smale of which you can see in the [example.template](templates/example.template):

```json
{
  "AWSTemplateFormatVersion": "2010-09-09",
  "Transform": [
    "ForEach", # should be _before_ the serverless transform
    "AWS::Serverless-2016-10-31"
  ],
  "Description": "Uses the macros.",
  "Parameters": {
    "ListA": {
        "Type": "CommaDelimitedList", # required type
        "Default": "a, b, c"
    },
    "ListB": {
        "Type": "CommaDelimitedList",
        "Default": "X, YY, ZZZ"
    }
  },
  "Resources": {
    "A%d": {
        "Type": "AWS::S3::Bucket",
        "Properties": {
            "BucketName": "random-bucket-for-lee-%v"
        },
        "ForEach": "ListA" # Name of parameter. 
    },
    "B%v": {
        "Type": "AWS::Serverless::SimpleTable",
        "DependsOn": [ "A%d" ],
        "Properties": {
            "PrimaryKey": {
                "Type": "String",
                "Name": "table-%d-key-%v"
            }
        },
        "ForEach": "ListB" # Using a different parameter here.
    }
  }
}
```

This is a small example, but there is a lot going on:

* Two template parameters of type CommaDelimitedList are declared.  
* Two "prototype" or "template" resources are declared.  
* Each of the prototype resources uses a different parameter, and will be patterned based on the respective values.
* The resource with the key "A%d" (which isn't a valid cloudformation resource key name as it sits) will become three resources:
    * Keys `A0` through `A2`
    * BucketNames `random-bucket-for-lee-a`, `random-bucket-for-lee-b` and `random-bucket-for-lee-c`
* The resource with the key "B%v" will become three resources:
    * Keys `BX`, `BYY` and `BZZZ`
    * DependsOn properties `A0`, `A1` and `A2`. This is pretty cool - the tables reference resources generated by the ForEach for ListA. This works out since both lists have the same number of items, so the `%d` substitution ends up being a correllating number.
    * PrimaryKey.Names that vary for each table (who would do that?).

When transformed, the above template becomes:

```json
{
  "AWSTemplateFormatVersion": "2010-09-09",
  "Description": "Uses the macros.",
  "Parameters": {
    "ListA": {
      "Type": "CommaDelimitedList",
      "Default": "a, b, c"
    },
    "ListB": {
      "Type": "CommaDelimitedList",
      "Default": "X, YY, ZZZ"
    }
  },
  "Resources": {
    "A0": {
      "Type": "AWS::S3::Bucket",
      "Properties": {
        "BucketName": "random-bucket-for-lee-a"
      }
    },
    "A1": {
      "Type": "AWS::S3::Bucket",
      "Properties": {
        "BucketName": "random-bucket-for-lee-b"
      }
    },
    "A2": {
      "Type": "AWS::S3::Bucket",
      "Properties": {
        "BucketName": "random-bucket-for-lee-c"
      }
    },
    "BX": {
      "Type": "AWS::DynamoDB::Table",
      "DependsOn": [
        "A0"
      ],
      "Properties": {
        "AttributeDefinitions": [
          {
            "AttributeName": "table-0-key-X",
            "AttributeType": "S"
          }
        ],
        "KeySchema": [
          {
            "AttributeName": "table-0-key-X",
            "KeyType": "HASH"
          }
        ],
        "BillingMode": "PAY_PER_REQUEST"
      }
    },
    "BYY": {
      "Type": "AWS::DynamoDB::Table",
      "DependsOn": [
        "A1"
      ],
      "Properties": {
        "AttributeDefinitions": [
          {
            "AttributeName": "table-1-key-YY",
            "AttributeType": "S"
          }
        ],
        "KeySchema": [
          {
            "AttributeName": "table-1-key-YY",
            "KeyType": "HASH"
          }
        ],
        "BillingMode": "PAY_PER_REQUEST"
      }
    },
    "BZZZ": {
      "Type": "AWS::DynamoDB::Table",
      "DependsOn": [
        "A2"
      ],
      "Properties": {
        "AttributeDefinitions": [
          {
            "AttributeName": "table-2-key-ZZZ",
            "AttributeType": "S"
          }
        ],
        "KeySchema": [
          {
            "AttributeName": "table-2-key-ZZZ",
            "KeyType": "HASH"
          }
        ],
        "BillingMode": "PAY_PER_REQUEST"
      }
    }
  }
}
```

## License

Published under the MIT license. Use these macros as you will, but maybe drop me a note or credit if you find them awesome.
