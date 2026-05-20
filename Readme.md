# Cloudformation Macros

This repo contains macros that help create more concise and easier to maintain CloudFormation and SAM templates. See below for details on each of the macros herewithin. You might also be interested in [the macros and tranformations AWS provides](https://github.com/aws-cloudformation/aws-cloudformation-macros).  


## ForEach

This macro is similar to the `Fn::ForEach` macro in AWS SAM (aka. Language Extensions). It differs a little, adding the ability to dynamically build arrays as well as objects, and focuses on allowing you to specify a comma-deliminted list parameter to instantiate a "template" object once for each item in the list. The macro will replace the template object with the generated instances (so it works in objects for properties and in arrays for values).

### Template Object Syntax

Any JSON object within the `Resources` section of your Cloudformation file can be a template.  To indicate that an object should be used as a template simply add a property to the object called `ForEach` and set it to the name of a parameter of type `CommaDelimitedList`. 

```jsonc
{
  "AWSTemplateFormatVersion": "2010-09-09",
  "Description": "Uses the ForEach macro to create buckets named 'bucket-a', 'bucket-b' and 'bucket-c'.",
  "Transform": [ 
    "ForEach"
  ],
  "Parameters": {
    "List": { // source of values to use for template instances
      "Type": "CommaDelimitedList",
      "Default": "a, b, c"
    }
  },
  "Resources": {
    "MyBucket": {
      "Type": "AWS::S3::Bucket",
      "ForEach": "List", // signal `MyBucket` is a template, reference to parameter
      "Properties": {
        "BucketName": "bucket-%v"
      }
    }
  }
}
```

The template will be instantiated once for each value in the list. When instantiating the template, appearances of `%v` in the template will be replaced with the item value, and appearances of `%d` replaced with the index of the item.  In the example above the template would create three buckets named `bucket-a`, `bucket-b` and `bucket-c` with logical identifiers `MyBucketa`, `MyBucketb` and `MyBucketc`, respectively.

### Template Objects as Property Values

Using `%d` or `%v` placeholders in property keys causes `cfn-lint` to complain because resource logical keys MUST be alphanumeric. If the template fails validation AWS SAM prohibits deploying the template, so an alternative was needed. 

Our solution is that `ForEach` allows you to use a valid alphanumeric key and choose to use either `%d` and `%v` as the **implicit suffix** for the keys of the patterned instances. The default behavior is to use `%v` (the value from the list) as the suffix for the keys (this is safer than using the index, see the [words of caution](#caveats), below). To specify the behavior, add the property `ResourceKeySuffix` to the `ForEach` object called and set it to either `Index` or `Value`:

```json
{
  "AWSTemplateFormatVersion": "2010-09-09",
  "Description": "Same as above, but property names (keys) are suffixed with indexes.",
  "Transform": [ 
    "ForEach"
  ],
  "Parameters": {
    "List": {
      "Type": "CommaDelimitedList",
      "Default": "a, b, c"
    }
  },
  "Resources": {
    "MyBucket": {
      "Type": "AWS::S3::Bucket",
      "ForEach": {
        "List": "List",
        "ResourceKeySuffix": "Index"
      },
      "Properties": {
        "BucketName": "bucket-%v"
      }
    }
  }
}
```
The above template will create buckets with the names `bucket-a`, `bucket-b` and `bucket-c` with logical identifiers `MyBucket1`, `MyBucket2` and `MyBucket3`, respectively.

### Template Objects in Arrays

Templates may also be placed in arrays, in which case the template will be replaced with the generated instances. Other pre-existing items in the array will be left in place. For example:

```jsonc
{
  "AWSTemplateFormatVersion": "2010-09-09",
  "Description": "Same as above, but property names (keys) are suffixed with indexes.",
  "Transform": [ 
    "ForEach"
  ],
  "Parameters": {
    "AdditionalAttributeNames": {
      "Type": "CommaDelimitedList",
      "Default": "MyAttribute1, MyAttribute2"
    }
  },
  "Resources": {
    "MyTable": {
      "Type": "AWS::DynamoDB::Table",
      "Properties": {
        ...,
        "AttributeDefinitions": [
          {
            "AttributeName": "PK",
            "AttributeType": "S"
          },
          {
            "AttributeName": "SK",
            "AttributeType": "S"
          },
          {
            "AttributeName": "%v",
            "AttributeType": "S",
            "ForEach": "AdditionalAttributeNames"
          }
        ],
        "KeySchema": [
          {
            "AttributeName": "PK",
            "KeyType": "HASH"
          },
          {
            "AttributeName": "SK",
            "KeyType": "RANGE"
          }
        ]
      }
    }
  }
}
```

The above temlate will create a table with fixed keys `PK` and `SK` and zero or more additional attributes as specified in the `AdditionalAttributeNames` parameter.  This isn't possible with the AWS SAM `Fn::ForEach` macro and is the primary reason this alternative exists.

### Example Template Explainer

The `ForEach` macro allows for more concise and easier to maintain templates. A simple example of which you can see in [example.template](templates/example.template), where there is a bit going on:

* Three template parameters of type CommaDelimitedList are declared.  
* Two "template" resources are declared to create a set of buckets and DDB tables.  
* The prototype resources use different list parameters and will be patterned based on their respective values.
* The resource template with the key "A" uses the default suffix (item value, transformed to remove characters that aren't legal in resource names) will become three resources:
    * Logical Names `Aa`, `Ab` and `Ac`
    * BucketNames `random-bucket-for-lee-a`, `random-bucket-for-lee-b` and `random-bucket-for-lee-c`
    * The value of the property `Rules` is an array containing a template object that will be patterned based on the list parameter `ListC`. 
* The resource with the key "B" (which is configured to use the "Index" suffix) will become three resources:
    * Keys `B0`, `B1` and `B2`. Note the configuration of the ForEach indicates the index should be appended to the key name. I don't recommend this approach (see below).
    * `PrimaryKey.Names` that vary for each table.

For reference, the [exaple-after-transorm.template](templates/example-after-transform.template) contains the example template as it appears after being transformed and as CloudFormation will deploy it.  

## Deployment

This is a conventional AWS SAM project, so the usual `sam build && sam deploy` command can be used to deploy it from a local CLI. 

In addition, there is a [deploy script](scripts/deploy-global.sh) in the scripts folder that can be used, or referenced. The script makes use of some environment variables you will need to set:

* `BUCKET_NAME_PREFIX` - The prefix for the bucket name where the SAM artifacts will be uploaded. In each region you deploy the macro to, a bucket must exist with the name `${BUCKET_NAME_PREFIX}-${AWS_REGION}`.
* `BUCKET_KEY_PREFIX` - The prefix for the key in the bucket where the SAM artifacts will be uploaded.

NOTE: The script will deploy the macro to *all regions* enabled in your AWS account.  If you want to deploy to only selected regions, modify the script as needed (see the references to `ACCOUNT_REGIONS`).

This project is also setup for CodeBuild with a `buildspec.yml` file, so you can setup a CodeBuild project to deploy the macro. If going thise route, I *strongly* recommend only triggering such a CodeBuild from a fork of this repo, or manually -- not directly from this repo. The CodeBuild project will need to be configured for [batch builds](https://docs.aws.amazon.com/codebuild/latest/userguide/batch-build.html).

## Some Words of Caution

While it's supported by `ForEach`, I DON'T recommend using `%d` (the index of the item in the list) as the suffix for your resource keys. When `%d` is used in that way and you re-order the list or remove an item from anywhere but the end of the list you may run into problems.

For example, consider what happens when you change a list paramter containing "x,y,z" to conyain "x,z" and then re-deploy:

* The initial deployment will create resources with keys like "A0", "A1" and "A2".
* The re-deploy will have resources "A0" and "A1" but the resource "A1" will now have the values generated from "z" rather than "y". 
* Some resources don't allow updates, and instead will delete the old resource if you change something (e.g. a record set name), so the re-deploy will try to first create "A1" with the values generated from "z" but since the original "A2" still exists, Route53 will error-out (duplicate names).

To avoid this, I recommend using the string value `%v` as the suffix (the default behavior) rather than the index as it unambigously maps the list item to the logical resource name.

## License

Published under the MIT license. Use these macros as you will, but maybe drop me a note or some public attribution if you find them helpful.
