# Cloudformation Macros

This repo contains macros that help me create more concise templates.  AWS provides [some macros and tranformations](https://github.com/aws-cloudformation/aws-cloudformation-macros) as well, but they don't cover some obvious use cases.  See below for details on the macros herewithin.

## Deployment

This is an AWS SAM project, so it's the usual `sam build && sam deploy`.  There is a [deploy script](scripts/deploy-global.sh) in the scripts folder but since this project is setup for CodeBuild and using ParameterStore you might need to configure some parameters to match your own bucket name/prefix.

## ForEach

This macro is similar to the `Count` macro from AWS, but instead of simply using an incrementing number it allows you to specific a comma deliminted list parameter and instantiates the annotated object once for each item in the list. It does the same replacement of `%d` with the instance index number, but will also replace `%v` with the value from the list. However, using those placeholders in resource keys (logical names) presents a problem since those key MUST be alphanumeric, and if your template needs to pass validation using cfn-lint or deploying via AWS SAM, then an alternative is needed.

So, ForEach allows you to use a valid alphanumeric key and configure using either `%d` and `%v` as the generated suffix for the keys of the patterned instances. This allows some pretty convenient patterns, an example of which you can see in [example.template](templates/example.template), where there is a bit going on:

* Two template parameters of type CommaDelimitedList are declared.  
* Two "prototype/template" resources are declared.  
* The prototype resources use different parameters, and will be patterned based on the respective values.
* The resource with the key "A" (which uses the default suffix) will become three resources:
    * Keys `Aa`, `Ab` and `Ac`
    * BucketNames `random-bucket-for-lee-a`, `random-bucket-for-lee-b` and `random-bucket-for-lee-c`
* The resource with the key "B" (which is configured to use the "Index" suffix) will become three resources:
    * Keys `B0`, `B1` and `B2`. Note the configuration of the ForEach indicates the index should be appended to the key name. I don't recommend this approach.
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
    "Aa": {
      "Type": "AWS::S3::Bucket",
      "Properties": {
        "BucketName": "random-bucket-for-lee-a"
      }
    },
    "Ab": {
      "Type": "AWS::S3::Bucket",
      "Properties": {
        "BucketName": "random-bucket-for-lee-b"
      }
    },
    "Ac": {
      "Type": "AWS::S3::Bucket",
      "Properties": {
        "BucketName": "random-bucket-for-lee-c"
      }
    },
    "B0": {
      "Type": "AWS::DynamoDB::Table",
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
    "B1": {
      "Type": "AWS::DynamoDB::Table",
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
    "B2": {
      "Type": "AWS::DynamoDB::Table",
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

## Caution

While it's supported, I recommend you DON'T use `%d` (index of the item in the list) as the suffix for your resource keys. If you do, then if you re-order the list or remove an item from anywhere but the end of the list you may run into problems.

When using `%d`, consider what happens when you change a list paramter containing "x,y,z" to conyain "x,z" and then re-deploy:

* The initial deployment will create resources with keys like "A0", "A1" and "A2".
* The re-deploy will have resources "A0" and "A1" but the resource "A1" will now have the values generated from "z" rather than "y". 
* Some resources don't allow updates, and instead will delete the old resource if you change something (e.g. a record set name), so the re-deploy will try to first create "A1" with the values generated from "z" but since the original "A2" still exists, Route53 will error-out (duplicate names).

To avoid this, always use the string value as the suffix rather than the index if at all possible (or at least understand and be prepared for the complications of using indexes and redeploying with edited lists).

## License

Published under the MIT license. Use these macros as you will, but maybe drop me a note or credit if you find them awesome.
